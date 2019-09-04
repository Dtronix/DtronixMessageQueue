using System;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.TcpSocket;

//using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TlsSocket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    public abstract class TlsSocketSession : IDisposable
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

        private TlsSocketConfig _config;

        /// <summary>
        /// Configurations for the associated socket.
        /// </summary>
        public TlsSocketConfig Config => _config;

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
        //public TcpSocketHandler<TSession, TConfig> SocketHandler { get; private set; }

        /// <summary>
        /// Contains the version number of the protocol used by the other end of the connection.
        /// </summary>
        public byte OtherProtocolVersion { get; private set; }

        public TcpSocketMode Mode { get; }

        /// <summary>
        /// Processor to handle all inbound messages.
        /// </summary>
        protected ActionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Processor to handle all outbound messages.
        /// </summary>
        protected ActionProcessor<Guid> OutboxProcessor;


        



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
        public event EventHandler<TlsSocketSessionEventArgs> Connected;

        /// <summary>
        /// This event fires when a connection has been shutdown.
        /// </summary>
        public event EventHandler<TlsSocketSessionClosedEventArgs> Closed;

        /// <summary>
        /// Creates a new socket session with a new Id.
        /// </summary>
        protected TlsSocketSession(TlsSocketSessionCreateArguments args)
        {
            Id = Guid.NewGuid();
            CurrentState = State.Closed;
            Mode = args.Mode;
            _config = args.SessionConfig;
            _socket = args.SessionSocket;
            _writeSemaphore = new SemaphoreSlim(1, 1);

            InboxProcessor = args.InboxProcessor;
            OutboxProcessor = args.OutboxProcessor;
            //ServiceMethodCache = args.ServiceMethodCache,

        }

        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        public void StartSession()
        {
            if (CurrentState != State.Closed)
                return;

            // Change into a securing state for the 
            CurrentState = State.Securing;
            ConnectedTime = DateTime.UtcNow;

            // Start receiving data for the secured channel.
            var result = _socket.ReceiveAsync(_receiveArgs);

            // TODO: REMOVE ---------------
            CurrentState = State.Connected;
            OnConnected();
            // TODO -----------------------

            /*// Send the protocol version number along with the public key to the connected client.
            if (SocketHandler.Mode == TcpSocketMode.Client)
            {

                // Authenticate client
            }
            */
        }

        /// <summary>
        /// Called when this session is connected to the socket.
        /// </summary>
        protected virtual void OnConnected()
        {
            //logger.Info("Session {0}: Connected", Id);
            Connected?.Invoke(this, new TlsSocketSessionEventArgs(this));
        }

        /// <summary>
        /// Called when this session is disconnected from the socket.
        /// </summary>
        /// <param name="reason">Reason this socket is disconnecting</param>
        protected virtual void OnDisconnected(CloseReason reason)
        {
            Closed?.Invoke(this, new TlsSocketSessionClosedEventArgs(this, reason));
        }

        /// <summary>
        /// Overridden to parse incoming bytes from the wire.
        /// </summary>
        /// <param name="buffer">Buffer of bytes to parse.</param>
        protected abstract void HandleIncomingBytes(ReadOnlyMemory<byte> buffer);

        

        

        private bool ReceiveCompleteInternal(SocketAsyncEventArgs e)
        {

            HandleIncomingBytes(_receiveArgs.MemoryBuffer.Slice(0, e.BytesTransferred));
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

            _config.Logger?.Trace($"{Mode}: Connection closed. Reason: {reason}.");

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
            _sendArgs.Free();
            _receiveArgs.Free();


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
            return $"{Mode} TlsSocketSession;";
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