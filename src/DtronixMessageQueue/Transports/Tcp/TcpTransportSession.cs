using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DtronixMessageQueue.Transports.Tcp
{
    public class TcpTransportSession : ITransportSession
    {
        public event EventHandler<TransportReceiveEventArgs> Received;
        public event EventHandler<TransportSessionEventArgs> Sent;
        public event EventHandler<TransportSessionEventArgs> Disconnected;
        public event EventHandler<TransportSessionEventArgs> Connected;


        public TransportState State { get; private set; }
        public TransportMode Mode { get; } = TransportMode.Unset;

        private Socket _socket;

        private readonly TransportConfig _config;

        /// <summary>
        /// Async args used to send data to the wire.
        /// </summary>
        private TcpTransportAsyncEventArgs _sendArgs;

        /// <summary>
        /// Async args used to receive data off the wire.
        /// </summary>
        private TcpTransportAsyncEventArgs _receiveArgs;

        /// <summary>
        /// Reset event used to ensure only one MqWorker can write to the socket at a time.
        /// </summary>
        private SemaphoreSlim _writeSemaphore;

        public TcpTransportSession(Socket socket, TransportConfig config, BufferMemoryPool memoryPool)
        {
            State = TransportState.Unknown;
            _socket = socket;
            _config = config;
            _writeSemaphore = new SemaphoreSlim(1,1);

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
            if (State != TransportState.Unknown)
                return;

            if (!_socket.ReceiveAsync(_receiveArgs))
            {
                IoCompleted(this, _receiveArgs);
            }

            State = TransportState.Connected;

            Connected?.Invoke(this, new TransportSessionEventArgs(this));
        }

        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer to copy and send.</param>
        public bool Send(ReadOnlyMemory<byte> buffer)
        {
            if (_socket == null || _socket.Connected == false)
                return false;

            if (buffer.Length > _config.SendAndReceiveBufferSize)
            {
                _config.Logger?.Error($"{Mode}: Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_config.SendAndReceiveBufferSize}].");
                throw new Exception($"Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_config.SendAndReceiveBufferSize}].");
            }
            _writeSemaphore.Wait(-1);

            var remaining = _sendArgs.Write(buffer);
            _sendArgs.PrepareForSend();

            try
            {
                // Set the data to be sent.
                if (!_socket.SendAsync(_sendArgs))
                {
                    // Send process occured synchronously
                    SendComplete(_sendArgs);
                }

                return true;
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
                return false;
            }


        }



        public void Disconnect()
        {
            // If this session has already been closed, nothing more to do.
            if (State == TransportState.Closed)
                return;

            _config.Logger?.Trace($"{Mode}: Connection closed.");


            _sendArgs.Completed -= IoCompleted;
            _receiveArgs.Completed -= IoCompleted;

            // close the socket associated with the client
            try
            {
                _socket.Shutdown(SocketShutdown.Receive);
                _socket.Disconnect(false);


                _sendArgs?.Free();
                _receiveArgs?.Free();
                _writeSemaphore?.Dispose();

                State = TransportState.Closed;
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _socket.Close(1000);

            }

            Disconnected?.Invoke(this, new TransportSessionEventArgs(this));
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
                    _config.Logger?.Error($"{Mode}: The last operation completed on the socket was not a receive, send connect or disconnect. {e.LastOperation}");
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
            if (e.SocketError != SocketError.Success)
            {
                Disconnect();
            }

            // Reset the socket args for sending again.
            _sendArgs.ResetSend();

            _config.Logger?.Trace($"{Mode}: Sending {e.BytesTransferred} bytes complete. Releasing Semaphore...");
            _writeSemaphore.Release(1);
            _config.Logger?.Trace($"{Mode}: Released semaphore.");

            Sent?.Invoke(this, new TransportSessionEventArgs(this));
        }



        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        private void ReceiveComplete(SocketAsyncEventArgs e)
        {
            _config.Logger?.Trace($"{Mode}: Received {e.BytesTransferred} bytes.");
            if (State == TransportState.Closed)
                return;

            if (e.BytesTransferred == 0)
            {
                Disconnect();
                return;
            }

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // If the session was closed curing the internal receive, don't read any more.
                Received?.Invoke(this, new TransportReceiveEventArgs(
                    e.MemoryBuffer.Slice(0, e.BytesTransferred)));

                try
                {
                    // Re-setup the receive async call.
                    if (!_socket.ReceiveAsync(e))
                    {
                        IoCompleted(this, e);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
