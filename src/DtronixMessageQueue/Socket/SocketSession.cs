using System;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base socket session to be sub-classes by the implementer.
    /// </summary>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    /// <typeparam name="TSession">Session for this connection.</typeparam>
    public abstract class SocketSession<TSession, TConfig> : IDisposable
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : TransportLayerConfig
    {
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
        public SessionHandler<TSession, TConfig> SessionHandler { get; private set; }


        public ITransportLayerSession TransportSession { get; private set; }

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
        }

        /// <summary>
        /// Sets up this socket with the specified configurations.
        /// </summary>
        /// <param name="sessionSocket">Socket this session is to use.</param>
        /// <param name="socketArgsManager">Argument pool for this session to use.  Pulls two asyncevents for reading and writing and returns them at the end of this socket's life.</param>
        /// <param name="sessionConfig">Socket configurations this session is to use.</param>
        /// <param name="sessionHandler">Handler base which is handling this session.</param>
        /// <param name="inboxProcessor">Processor which handles all inbox data.</param>
        /// /// <param name="outboxProcessor">Processor which handles all outbox data.</param>
        /// <param name="serviceMethodCache">Cache for commonly called methods used throughout the session.</param>
        public static TSession Create(
            ITransportLayerSession transportSession,
            TConfig sessionConfig, 
            SessionHandler<TSession, TConfig> sessionHandler)
        {
            var session = new TSession
            {
                TransportSession = transportSession,
                _config = sessionConfig,
                _writeSemaphore = new SemaphoreSlim(1, 1),
                ConnectedTime = DateTime.UtcNow,
                SessionHandler = sessionHandler,
            };

            session.OnSetup();

            return session;
        }


        /// <summary>
        /// Called after the initial setup has occurred on the session.
        /// </summary>
        protected abstract void OnSetup();

        public virtual void Connect()
        {
            // Start receiving data.
            TransportSession.StartReceieve();

            OnConnected();
        }

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
        protected virtual void OnClose(SessionCloseReason reason)
        {
            Closed?.Invoke(this, new SessionClosedEventArgs<TSession, TConfig>((TSession)this, reason));
        }

        /// <summary>
        /// Overridden to parse incoming bytes from the wire.
        /// </summary>
        /// <param name="buffer">Buffer of bytes to parse.</param>
        protected abstract void OnReceive(byte[] buffer);


        /// <summary>
        /// Called when this session is desired or requested to be closed.
        /// </summary>
        /// <param name="reason">Reason this socket is closing.</param>
        public virtual void Close(SessionCloseReason reason)
        {


            // Notify the session has been closed.
            OnClose(reason);
        }

        /// <summary>
        /// Disconnects client and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (CurrentState == State.Connected)
                Close(SessionCloseReason.ClientClosing);

        }
    }
}