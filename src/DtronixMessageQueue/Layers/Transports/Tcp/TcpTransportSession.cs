using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DtronixMessageQueue.Layers.Transports.Tcp
{
    public class TcpTransportSession : ITransportSession
    {
        public Action<ReadOnlyMemory<byte>> Received { get; set; }

        public Action<ISession> Sent { get; set; }

        public event EventHandler<SessionEventArgs> Disconnected;
        public event EventHandler<SessionEventArgs> Connected;
        public event EventHandler<SessionEventArgs> Ready;

        public ISession WrapperSession { get; set; }

        public SessionMode Mode { get; }

        public SessionState State { get; private set; } = SessionState.Unknown;

        private readonly Socket _socket;

        private readonly TransportConfig _config;

        /// <summary>
        /// Async args used to send data to the wire.
        /// </summary>
        private readonly TcpTransportAsyncEventArgs _sendArgs;

        /// <summary>
        /// Async args used to receive data off the wire.
        /// </summary>
        private readonly TcpTransportAsyncEventArgs _receiveArgs;

        /// <summary>
        /// Reset event used to ensure only one MqWorker can write to the socket at a time.
        /// </summary>
        private readonly SemaphoreSlim _writeSemaphore;

        public TcpTransportSession(
            Socket socket, 
            TransportConfig config, 
            BufferMemoryPool memoryPool, 
            SessionMode mode)
        {
            _socket = socket;
            _config = config;
            _writeSemaphore = new SemaphoreSlim(1, 1);
            Mode = mode;

            _sendArgs = new TcpTransportAsyncEventArgs(memoryPool);
            _receiveArgs = new TcpTransportAsyncEventArgs(memoryPool);
            _sendArgs.Completed += IoCompleted;
            _receiveArgs.Completed += IoCompleted;

            if (_config.SendTimeout > 0)
                _socket.SendTimeout = _config.SendTimeout;

            if (_config.SendAndReceiveBufferSize > 0)
                _socket.ReceiveBufferSize = _config.SendAndReceiveBufferSize;

            if (_config.SendAndReceiveBufferSize > 0)
                _socket.SendBufferSize = _config.SendAndReceiveBufferSize;

            _socket.NoDelay = true;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
        }


        public void Connect()
        {
            if (State != SessionState.Unknown)
            {
                _config.Logger?.Error($"{Mode} Attempt to connect when session is in {State} state.");
                return;
            }

            _config.Logger?.Trace($"{Mode} Session starts receiving data.");

            State = SessionState.Connected;

            _config.Logger?.Trace($"{Mode} Session Connected event fired." +
                                  (Disconnected == null ? " No Listeners" : ""));

            // Functionally, ready and connected function the same in this TCP transport.
            Connected?.Invoke(this, new SessionEventArgs(this));
            Ready?.Invoke(this, new SessionEventArgs(this));

            StartReceive();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartReceive()
        {
            try
            {
                // This will sometimes complete synchronously and receive data before the connected
                // event is fired.  Connected will need to be called before receiving has started.
                _config.Logger?.Trace($"{Mode} Session starts receiving data.");
                // Re-setup the receive async call.
                if (!_socket.ReceiveAsync(_receiveArgs))
                {
                    _config.Logger?.Trace($"{Mode} Session receive completed synchronously.");
                    IoCompleted(this, _receiveArgs);
                }
            }
            catch (ObjectDisposedException e)
            {
                _config.Logger?.Trace($"{Mode} Session was closed when receiving data. {e.Message}");
                Disconnect();
            }
            catch (Exception e)
            {
                _config.Logger?.Error($"{Mode} Session exception occured while receiving. {e.Message}");
                Disconnect();
            }
        }

        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer to copy and send.</param>
        /// <param name="flush"></param>
        public void Send(ReadOnlyMemory<byte> buffer, bool flush)
        {
            if (_socket == null || _socket.Connected == false)
            {
                _config.Logger?.Error($"{Mode} Session attempted to send buffer when session is not connected.");
                return;
            }

            if (buffer.Length > _config.SendAndReceiveBufferSize)
            {
                _config.Logger?.Error(
                    $"Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_config.SendAndReceiveBufferSize}].");
                throw new Exception(
                    $"Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_config.SendAndReceiveBufferSize}].");
            }

            _config.Logger?.Trace($"{Mode} Acquiring write semaphore...");
            _writeSemaphore.Wait(-1);
            _config.Logger?.Trace($"{Mode} Acquired write semaphore.");

            var remaining = _sendArgs.Write(buffer);
            _sendArgs.PrepareForSend();

            try
            {
                _config.Logger?.Trace($"{Mode} Session starts sending data.");
                // Set the data to be sent.
                if (!_socket.SendAsync(_sendArgs))
                {
                    _config.Logger?.Trace($"{Mode} Session completed synchronously.");
                    // Send process occured synchronously
                    SendComplete(_sendArgs);
                }
            }
            catch (ObjectDisposedException e)
            {
                _config.Logger?.Trace($"{Mode} Session was closed when sending data. {e.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _config.Logger?.Trace($"{Mode} Session Disconnect() called.");

            // If this session has already been closed, nothing more to do.
            if (State != SessionState.Connected)
            {
                _config.Logger?.Error($"{Mode} Session attempted to close when not open.");
                return;
            }

            _sendArgs.Completed -= IoCompleted;
            _receiveArgs.Completed -= IoCompleted;

            // close the socket associated with the client
            try
            {
                _config.Logger?.Trace($"{Mode} Attempting to close session socket...");
                _socket.Shutdown(SocketShutdown.Receive);
                _socket.Disconnect(false);
            }
            catch (Exception e)
            {
                // ignored
                _config.Logger?.Trace($"{Mode} Session closing error {e}");
            }
            finally
            {
                _sendArgs?.Free();
                _receiveArgs?.Free();
                _writeSemaphore?.Dispose();

                State = SessionState.Closed;
                _socket.Close(1000);
                _config.Logger?.Trace($"{Mode} Session socket closed.");
            }

            _config.Logger?.Trace($"{Mode} Session Disconnected event fired." + (Disconnected == null ? " No Listeners" : ""));
            Disconnected?.Invoke(this, new SessionEventArgs(this));
        }

        /// <summary>
        /// This method is called whenever a receive or send operation is completed on a socket 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        protected virtual void IoCompleted(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Disconnect:
                    Disconnect();
                    break;

                case SocketAsyncOperation.Receive:
                    ReceiveComplete(e);
                    break;

                case SocketAsyncOperation.Send:
                    SendComplete(e);
                    break;

                default:
                    _config.Logger?.Error(
                        $"{Mode} The last operation completed on the socket was not a receive, send connect or disconnect. {e.LastOperation}");
                    Disconnect();
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive, send connect or disconnect.");
            }
        }

        /// <summary>
        /// This method is invoked when an asynchronous send operation completes.  
        /// The method issues another receive on the socket to read any additional data sent from the client
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        private void SendComplete(SocketAsyncEventArgs e)
        {
            _config.Logger?.Trace($"{Mode} Session sending completed.");
            if (e.SocketError != SocketError.Success)
            {
                _config.Logger?.Error($"{Mode} Session sending encountered error. Socket Error: {e.SocketError}.");
                Disconnect();
            }

            // Reset the socket args for sending again.
            _sendArgs.ResetSend();

            _config.Logger?.Trace($"{Mode} Sending {e.BytesTransferred} bytes complete. Releasing Semaphore...");
            _writeSemaphore.Release(1);
            _config.Logger?.Trace($"{Mode} Released semaphore.  Session Sent method called." + (Sent == null ? " No connected method." : ""));

            Sent?.Invoke(this);
        }


        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        private void ReceiveComplete(SocketAsyncEventArgs e)
        {
            _config.Logger?.Trace($"{Mode} Received {e.BytesTransferred} bytes.");
            if (State != SessionState.Connected)
            {
                _config.Logger?.Error($"{Mode} Session receive complete when session is not connected. State: {State}.");
                return;
            }

            if (e.SocketError != SocketError.Success)
            {
                _config.Logger?.Error($"{Mode} Session sending encountered error. Socket Error: {e.SocketError}.");
                Disconnect();
            }

            _config.Logger?.Trace($"{Mode} Session Received method called." + (Received == null ? " No connected method." : ""));
            Received?.Invoke(e.MemoryBuffer.Slice(0, e.BytesTransferred));

            if (e.BytesTransferred == 0)
            {
                _config.Logger?.Trace($"{Mode} Received 0 bytes.  Closing session.");
                Disconnect();
                return;
            }

            StartReceive();

        }
    }
}