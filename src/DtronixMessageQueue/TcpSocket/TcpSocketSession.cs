using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
//using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    /// <typeparam name="TSession">Session for this connection.</typeparam>
    public abstract class TcpSocketSession<TSession, TConfig> : IDisposable, ISetupSocketSession
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
        /// <summary>
        /// Current state of the socket.
        /// </summary>
        public enum State : byte
        {
            /// <summary>
            /// State has not been set.
            /// </summary>
            Unknown,

            /// <summary>
            /// Session is in the process of securing the communication channel.
            /// </summary>
            Securing,

            /// <summary>
            /// Session has connected to remote session.
            /// </summary>
            Connected,

            /// <summary>
            /// Session has been closed and no longer can be used.
            /// </summary>
            Closed,

            /// <summary>
            /// Socket is in an error state.
            /// </summary>
            Error,
        }

        public const byte ProtocolVersion = 1;

        private TConfig _config;

        /// <summary>
        /// Configurations for the associated socket.
        /// </summary>
        public TConfig Config => _config;

        /// <summary>
        /// Id for this session
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// State that this socket is in.  Can only perform most operations when the socket is in a Connected state.
        /// </summary>
        public State CurrentState { get; protected set; }

        /// <summary>
        /// The last time that this session received a message.
        /// </summary>
        private DateTime _lastReceived = DateTime.UtcNow;

        /// <summary>
        /// Last time the session received anything from the socket.  Time in UTC.
        /// </summary>
        public DateTime LastReceived => _lastReceived;

        /// <summary>
        /// Time that this session connected to the server.
        /// </summary>
        public DateTime ConnectedTime { get; private set; }

        /// <summary>
        /// Base socket for this session.
        /// </summary>
        public TcpSocketHandler<TSession, TConfig> SocketHandler { get; private set; }

        /// <summary>
        /// Contains the version number of the protocol used by the other end of the connection.
        /// </summary>
        public byte OtherProtocolVersion { get; private set; }

        /// <summary>
        /// Processor to handle all inbound messages.
        /// </summary>
        protected ActionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Processor to handle all outbound messages.
        /// </summary>
        protected ActionProcessor<Guid> OutboxProcessor;


        private Socket _socket;

        /// <summary>
        /// Raw socket for this session.
        /// </summary>
        public Socket Socket => _socket;

        /// <summary>
        /// Async args used to send data to the wire.
        /// </summary>
        private SocketAsyncEventArgs _sendArgs;

        /// <summary>
        /// Async args used to receive data off the wire.
        /// </summary>
        private SocketAsyncEventArgs _receiveArgs;

        /// <summary>
        /// Pool used by all the sessions on this SessionHandler.
        /// </summary>
        private SocketAsyncEventArgsManager _argsPool;

        /// <summary>
        /// Reset event used to ensure only one MqWorker can write to the socket at a time.
        /// </summary>
        private SemaphoreSlim _writeSemaphore;


        /// <summary>
        /// Cache for commonly called methods used throughout the session.
        /// </summary>
        //public ServiceMethodCache ServiceMethodCache;

        /// <summary>
        /// This event fires when a connection has been established.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

        /// <summary>
        /// This event fires when a connection has been shutdown.
        /// </summary>
        public event EventHandler<SessionClosedEventArgs<TSession, TConfig>> Closed;

        /// <summary>
        /// Creates a new socket session with a new Id.
        /// </summary>
        protected TcpSocketSession()
        {
            Id = Guid.NewGuid();
            CurrentState = State.Closed;
        }

        /// <summary>
        /// Sets up this socket with the specified configurations.
        /// </summary>
        /// <param name="args">Args to initialize the socket with.</param>
        public static TSession Create(TlsSocketSessionCreateArguments<TSession, TConfig> args)
        {
            var session = new TSession
            {
                _config = args.SessionConfig,
                _argsPool = args.SocketArgsManager,
                _socket = args.SessionSocket,
                _writeSemaphore = new SemaphoreSlim(1, 1),
                SocketHandler = args.TlsSocketHandler,
                _sendArgs = args.SocketArgsManager.Create(),
                _receiveArgs = args.SocketArgsManager.Create(),
                InboxProcessor = args.InboxProcessor,
                OutboxProcessor = args.OutboxProcessor,
                //ServiceMethodCache = args.ServiceMethodCache,
            };

            session._sendArgs.Completed += session.IoCompleted;
            session._receiveArgs.Completed += session.IoCompleted;

            if (session._config.SendTimeout > 0)
                session._socket.SendTimeout = session._config.SendTimeout;

            if (session._config.SendAndReceiveBufferSize > 0)
                session._socket.ReceiveBufferSize = session._config.SendAndReceiveBufferSize;

            if (session._config.SendAndReceiveBufferSize > 0)
                session._socket.SendBufferSize = session._config.SendAndReceiveBufferSize;

            session._socket.NoDelay = true;
            session._socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            session.OnSetup();

            return session;
        }

        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void ISetupSocketSession.StartSession()
        {
            if (CurrentState != State.Closed)
                return;

            // Change into a securing state for the 
            CurrentState = State.Securing;
            ConnectedTime = DateTime.UtcNow;

            // Start receiving data for the secured channel.
            _socket.ReceiveAsync(_receiveArgs);

            // Send the protocol version number along with the public key to the connected client.
            if (SocketHandler.Mode == TcpSocketMode.Client)
            {

                // Authenticate client
            }
            
        }

        private bool SecureConnectionReceive(byte[] buffer)
        {
            if (CurrentState == State.Closed)
                return false;

            if (CurrentState != State.Securing)
            {
                Close(CloseReason.ProtocolError);
                return false;
            }



                if (SocketHandler.Mode == TcpSocketMode.Server)
                {
                    _config.Logger?.Trace($"{SocketHandler.Mode}: Sending client public ECDH key.");
                }


                SecureConnectionComplete();
                return true;

        }



        private void SecureConnectionComplete()
        {
            // Set the state to connected.
            CurrentState = State.Connected;

            _config.Logger?.Trace($"{SocketHandler.Mode}: Securing complete.");
            OnConnected();
        }

        /// <summary>
        /// Called after the initial setup has occurred on the session.
        /// </summary>
        protected abstract void OnSetup();

        /// <summary>
        /// Called when this session is connected to the socket.
        /// </summary>
        protected virtual void OnConnected()
        {
            //logger.Info("Session {0}: Connected", Id);
            Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession)this));
        }

        /// <summary>
        /// Called when this session is disconnected from the socket.
        /// </summary>
        /// <param name="reason">Reason this socket is disconnecting</param>
        protected virtual void OnDisconnected(CloseReason reason)
        {
            Closed?.Invoke(this, new SessionClosedEventArgs<TSession, TConfig>((TSession)this, reason));
        }

        /// <summary>
        /// Overridden to parse incoming bytes from the wire.
        /// </summary>
        /// <param name="buffer">Buffer of bytes to parse.</param>
        protected abstract void HandleIncomingBytes(byte[] buffer);

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
                    Close(CloseReason.Closing);
                    break;

                case SocketAsyncOperation.Receive:
                    RecieveComplete(e);
                    break;

                case SocketAsyncOperation.Send:
                    SendComplete(e);
                    break;

                default:
                    _config.Logger?.Error($"{SocketHandler.Mode}: The last operation completed on the socket was not a receive, send connect or disconnect. {e.LastOperation}");
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive, send connect or disconnect.");
            }
        }

        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent to the underlying system to send.
        /// Before transport encryption has been established, any buffer size will be sent.
        /// After transport encryption has been established, only buffers in increments of 16.
        /// Excess will be buffered until the next write.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Total bytes to send.</param>
        /// <param name="pad">
        /// Set to true to pad the data in the to the next 16 byte alignment to allow for instant sending.
        /// Set to false to prevent padding.
        /// </param>
        protected virtual void Send(byte[] buffer, int offset, ushort count, bool pad)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            _config.Logger?.Trace($"{SocketHandler.Mode}: Sending {count} decrypted bytes.");

            var lengthBytes = BitConverter.GetBytes(count);
            byte[] bodyLengthBytes = {
                lengthBytes[0],
                lengthBytes[1]
            };
            
            // Send with FullMessage header.
            //SendWithHeader(Header.Type.BodyPayload, bodyLengthBytes, 0, 2, buffer, offset, count, pad);
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
                Close(CloseReason.SocketError);
            }

            _config.Logger?.Trace($"{SocketHandler.Mode}: Sending {e.BytesTransferred} bytes complete. Releasing Semaphore...");
            _writeSemaphore.Release(1);
            _config.Logger?.Trace($"{SocketHandler.Mode}: Released semaphore.");
        }



        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        protected void RecieveComplete(SocketAsyncEventArgs e)
        {
            _config.Logger?.Trace($"{SocketHandler.Mode}: Received {e.BytesTransferred} encrypted bytes.");
            if (CurrentState == State.Closed)
                return;

            if (e.BytesTransferred == 0)
            {
                HandleIncomingBytes(null);
                return;
            }

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // If the session was closed curing the internal receive, don't read any more.
                if (!ReceiveCompleteInternal(e.Buffer, e.Offset, e.BytesTransferred))
                    return;

                try
                {
                    // Re-setup the receive async call.
                    if (Socket.ReceiveAsync(e) == false)
                    {
                        IoCompleted(this, e);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Close(CloseReason.SocketError);
                }
            }
            else
            {
                Close(CloseReason.SocketError);
            }
        }

        private bool ReceiveCompleteInternal(byte[] buffer, int offset, int count)
        {
            

            return true;
        }

        /// <summary>
        /// Called when this session is desired or requested to be closed.
        /// </summary>
        /// <param name="reason">Reason this socket is closing.</param>
        public virtual void Close(CloseReason reason)
        {
            // If this session has already been closed, nothing more to do.
            if (CurrentState == State.Closed && reason != CloseReason.ConnectionRefused)
                return;

            _config.Logger?.Trace($"{SocketHandler.Mode}: Connection closed. Reason: {reason}.");

            CurrentState = State.Closed;

            // close the socket associated with the client
            try
            {
                if (Socket.Connected)
                {
                    // Alert the other end of the connection that the session has been closed.
                    //SendWithHeader(Header.Type.ConnectionClose, new[] {(byte) reason}, 0, 1, null, 0, 0, true);

                    Socket.Shutdown(SocketShutdown.Receive);
                    Socket.Disconnect(false);
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                Socket.Close(1000);
            }

            _sendArgs.Completed -= IoCompleted;
            _receiveArgs.Completed -= IoCompleted;

            // Free the SocketAsyncEventArg so they can be reused by another client.
            _argsPool.Free(_sendArgs);
            _argsPool.Free(_receiveArgs);


            InboxProcessor.Deregister(Id);
            OutboxProcessor.Deregister(Id);

            CurrentState = State.Closed;

            // Notify the session has been closed.
            OnDisconnected(reason);
        }

        /// <summary>
        /// String representation of the active session.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return $"{SocketHandler.Mode} TcpSocketSession;";
        }

        /// <summary>
        /// Disconnects client and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (CurrentState == State.Connected)
                Close(CloseReason.Closing);

        }
    }
}