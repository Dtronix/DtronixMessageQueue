using System;
using System.Net.Sockets;
using System.Threading;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    /// <typeparam name="TSession">Session for this connection.</typeparam>
    public abstract class SocketSession<TSession, TConfig> : IDisposable, ISetupSocketSession
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : SocketConfig
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
            /// Session is attempting to connect to remote connection.
            /// </summary>
            Connecting,

            /// <summary>
            /// Session has connected to remote session.
            /// </summary>
            Connected,

            /// <summary>
            /// Session is in the process of closing its connection.
            /// </summary>
            Closing,

            /// <summary>
            /// Session has been closed and no longer can be used.
            /// </summary>
            Closed,

            /// <summary>
            /// Socket is in an error state.
            /// </summary>
            Error
        }

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
        public SessionHandler<TSession, TConfig> BaseSocket { get; private set; }

        /// <summary>
        /// Processor to handle all inbound messages.
        /// </summary>
        protected SessionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Processor to handle all outbound messages.
        /// </summary>
        protected SessionProcessor<Guid> OutboxProcessor;


        private System.Net.Sockets.Socket _socket;

        /// <summary>
        /// Raw socket for this session.
        /// </summary>
        public System.Net.Sockets.Socket Socket => _socket;

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
        protected SocketSession()
        {
            Id = Guid.NewGuid();
            CurrentState = State.Connecting;
        }


        /// <summary>
        /// Sets up this socket with the specified configurations.
        /// </summary>
        /// <param name="sessionSocket">Socket this session is to use.</param>
        /// <param name="socketArgsManager">Argument pool for this session to use.  Pulls two asyncevents for reading and writing and returns them at the end of this socket's life.</param>
        /// <param name="sessionConfig">Socket configurations this session is to use.</param>
        /// <param name="sessionHandler">Handler base which is handling this session.</param>
        public static TSession Create(System.Net.Sockets.Socket sessionSocket, SocketAsyncEventArgsManager socketArgsManager,
            TConfig sessionConfig, SessionHandler<TSession, TConfig> sessionHandler, SessionProcessor<Guid> inboxProcessor,
            SessionProcessor<Guid> outboxProcessor)
        {
            var session = new TSession
            {
                _config = sessionConfig,
                _argsPool = socketArgsManager,
                _socket = sessionSocket,
                _writeSemaphore = new SemaphoreSlim(1, 1),
                BaseSocket = sessionHandler
            };

            session._sendArgs = session._argsPool.Create();
            session._sendArgs.Completed += session.IoCompleted;
            session._receiveArgs = session._argsPool.Create();
            session._receiveArgs.Completed += session.IoCompleted;

            session.InboxProcessor = inboxProcessor;
            inboxProcessor.Register(session.Id);

            session.OutboxProcessor = outboxProcessor;
            outboxProcessor.Register(session.Id);


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
        void ISetupSocketSession.Start()
        {
            if (CurrentState != State.Connecting)
            {
                return;
            }


            CurrentState = State.Connected;
            ConnectedTime = DateTime.UtcNow;

            // Start receiving data.
            _socket.ReceiveAsync(_receiveArgs);
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
        protected virtual void OnDisconnected(SocketCloseReason reason)
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
                    Close(SocketCloseReason.ClientClosing);
                    break;

                case SocketAsyncOperation.Receive:
                    RecieveComplete(e);

                    break;

                case SocketAsyncOperation.Send:
                    SendComplete(e);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive, send connect or disconnect.");
            }
        }

        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="length">Total bytes to send.</param>
        protected virtual void Send(byte[] buffer, int offset, int length)
        {
            if (Socket == null || Socket.Connected == false)
            {
                return;
            }
            _writeSemaphore.Wait(-1);

            // Copy the bytes to the block buffer
            Buffer.BlockCopy(buffer, offset, _sendArgs.Buffer, _sendArgs.Offset, length);

            // Update the buffer length.
            _sendArgs.SetBuffer(_sendArgs.Offset, length);

            try
            {
                if (Socket.SendAsync(_sendArgs) == false)
                {
                    IoCompleted(this, _sendArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                Close(SocketCloseReason.SocketError);
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
                Close(SocketCloseReason.SocketError);
            }
            _writeSemaphore.Release(1);
        }

        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        protected void RecieveComplete(SocketAsyncEventArgs e)
        {
            if (CurrentState == State.Closing)
            {
                return;
            }
            if (e.BytesTransferred == 0 && CurrentState == State.Connected)
            {
                CurrentState = State.Closing;
                return;
            }
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // Update the last time this session was active to prevent timeout.
                _lastReceived = DateTime.UtcNow;

                // Create a copy of these bytes.
                var buffer = new byte[e.BytesTransferred];

                Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

                HandleIncomingBytes(buffer);

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
                    Close(SocketCloseReason.SocketError);
                }
            }
            else
            {
                Close(SocketCloseReason.SocketError);
            }
        }

        /// <summary>
        /// Called when this session is desired or requested to be closed.
        /// </summary>
        /// <param name="reason">Reason this socket is closing.</param>
        public virtual void Close(SocketCloseReason reason)
        {
            // If this session has already been closed, nothing more to do.
            if (CurrentState == State.Closed)
            {
                return;
            }

            // close the socket associated with the client
            try
            {
                Socket.Shutdown(SocketShutdown.Receive);
                Socket.Disconnect(false);
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

            // Free the SocketAsyncEventArg so they can be reused by another client
            _argsPool.Free(_sendArgs);
            _argsPool.Free(_receiveArgs);

            InboxProcessor.Unregister(Id);
            OutboxProcessor.Unregister(Id);

            CurrentState = State.Closed;

            // Notify the session has been closed.
            OnDisconnected(reason);
        }

        /// <summary>
        /// Disconnects client and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (CurrentState == State.Connected)
            {
                Close(SocketCloseReason.ClientClosing);
            }
        }
    }
}