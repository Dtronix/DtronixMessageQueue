using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Base socket for all server and client sockets.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public abstract class MqSessionHandler<TSession, TConfig> : IDisposable
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Mode that this socket is running as.
        /// </summary>
        public TransportLayerMode LayerMode => TransportLayer.Mode;

        public TransportLayerState State => TransportLayer.State;


        protected ITransportLayer TransportLayer;

        /// <summary>
        /// This event fires when a connection has been established.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

        /// <summary>
        /// This event fires when a connection has been closed.
        /// </summary>
        public event EventHandler<SessionCloseEventArgs<TSession, TConfig>> Closed;

        /// <summary>
        /// Event fired when a new message arrives at the mailbox.
        /// </summary>
        public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

        /// <summary>
        /// Configurations of this socket.
        /// </summary>
        public TConfig Config { get; }

        /// <summary>
        /// Processor to handle all inbound and outbound message handling.
        /// </summary>
        protected ActionProcessor<Guid> OutboxProcessor;

        /// <summary>
        /// Processor to handle all inbound and outbound message handling.
        /// </summary>
        protected ActionProcessor<Guid> InboxProcessor;

        /// <summary>
        /// Dictionary of all connected clients.
        /// </summary>
        protected readonly ConcurrentDictionary<Guid, TSession> ConnectedSessions =
            new ConcurrentDictionary<Guid, TSession>();

        private readonly Timer _timeoutTimer;

        private bool _isTimeoutTimerRunning;

        /// <summary>
        /// Used to prevent more connections connecting to the server than allowed.
        /// </summary>
        private readonly object _connectionLock = new object();

        /// <summary>
        /// Set to the max number of connections allowed for the server.
        /// Decremented when a new connection occurs and incremented when 
        /// </summary>
        private int _remainingConnections;

        /// <summary>
        /// Base constructor to all socket classes.
        /// </summary>
        /// <param name="config">Configurations for this socket.</param>
        /// <param name="mode">Mode this session handler is to </param>
        protected MqSessionHandler(TConfig config, TransportLayerMode mode)
        {
            // Determine the type of transport layer to use.
            // If the transport layer is not defined, use the default Tcp layer.
            TransportLayer = config.TransportLayer ?? new TcpTransportLayer(config, mode);

            Config = config;

            _timeoutTimer = new Timer(OnTimeoutTimer);

            // Reset the remaining connections.
            _remainingConnections = Config.MaxConnections;

            var modeLower = mode.ToString().ToLower();

            var processorThreads = config.ProcessorThreads == -1
                ? Environment.ProcessorCount
                : config.ProcessorThreads;

            // Modify the max number of connections allowed if the handler is functioning in client mode.
            config.MaxConnections = mode == TransportLayerMode.Client ? 1 : config.MaxConnections;

            OutboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
            {
                ThreadName = $"{modeLower}-outbox",
                StartThreads = mode == TransportLayerMode.Client ? 1 : processorThreads
            });
            InboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
            {
                ThreadName = $"{modeLower}-inbox",
                StartThreads = mode == TransportLayerMode.Client ? 1 : processorThreads
            });

            TransportLayer.StateChanged += TransportLayerOnStateChanged;

            OutboxProcessor.Start();
            InboxProcessor.Start();
        }

        private void TransportLayerOnStateChanged(object o, TransportLayerStateChangedEventArgs e)
        {
            switch (e.State)
            {
                case TransportLayerState.Connected:
                    var maxSessions = false;

                    // Check if we are maxed out on concurrent connections.
                    // If so, stop listening for new connections until we can accept a new connection
                    lock (_connectionLock)
                    {

                        if (_remainingConnections == 0)
                        {
                            maxSessions = true;
                        }
                        else
                        {
                            _remainingConnections--;
                        }
                    }

                    // Convert the TransportLayerSession into a MqSession
                    var session = MqSession<TSession, TConfig>.Create(this, e.Session, InboxProcessor, OutboxProcessor);

                    // If we are at max sessions, close the new connection with a connection refused reason.
                    if (maxSessions)
                    {
                        session.Close(SessionCloseReason.ConnectionRefused);
                        return;
                    }

                    ConnectedSessions.TryAdd(session.Id, session);

                    session.IncomingMessage += (s, a) => IncomingMessage?.Invoke(this, a);
                    session.Closed += (s, a) => Closed?.Invoke(this, a);

                    // Invoke the outer connecting event.
                    OnConnected(session);
                    break;

                case TransportLayerState.Closed:

                    // Remove the session from the list of active sessions and release the semaphore.
                    if (e.Session != null && ConnectedSessions.TryRemove(e.Session.Id, out var connSession))
                    {
                        // If the remaining connection is now 1, that means that the server need to begin
                        // accepting new client connections.
                        lock (_connectionLock)
                            _remainingConnections++;
                    }


                    break;
            }
        }

        protected virtual void OnConnected(TSession session)
        {
            if (_isTimeoutTimerRunning)
                return;

            var timeout = Config.PingTimeout;

            if (timeout > 0 && !_isTimeoutTimerRunning)
            {
                _isTimeoutTimerRunning = true;
                _timeoutTimer.Change(timeout, timeout);
            }

            Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
        }

        protected virtual void OnClose(SessionCloseEventArgs<TSession, TConfig> closeEventArgs)
        {

            if (ConnectedSessions.Count == 0 && _isTimeoutTimerRunning)
            {
                _isTimeoutTimerRunning = false;
                _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            Closed?.Invoke(this, closeEventArgs);
        }


        protected virtual void OnTimeoutTimer(object state)
        {
            var past = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(Config.PingTimeout));
            foreach (var connectedSession in ConnectedSessions)
            {
                if (connectedSession.Value.LastReceived < past)
                    connectedSession.Value.Close(SessionCloseReason.TimeOut);
            }
        }

        public IEnumerator<KeyValuePair<Guid, TSession>> GetSessionsEnumerator()
        {
            return ConnectedSessions.GetEnumerator();
        }

        public void Dispose()
        {
            _timeoutTimer?.Dispose();
        }
    }
}