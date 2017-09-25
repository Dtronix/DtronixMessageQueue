using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base socket for all server and client sockets.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public abstract class SessionHandler<TSession, TConfig>
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : TransportLayerConfig
    {
        /// <summary>
        /// Mode that this socket is running as.
        /// </summary>
        public TransportLayerMode LayerMode => TransportLayer.Mode;


        protected ITransportLayer TransportLayer;

        /// <summary>
        /// True if the socket is connected/listening.
        /// </summary>
        public bool IsRunning => TransportLayer.IsRunning;

        /// <summary>
        /// This event fires when a connection has been established.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Connected;

        /// <summary>
        /// This event fires when a connection has been closed.
        /// </summary>
        public event EventHandler<SessionClosedEventArgs<TSession, TConfig>> Closed;

        /// <summary>
        /// Event called when a new session is created and is being setup but before the session is active.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> SessionSetup;

        /// <summary>
        /// Configurations of this socket.
        /// </summary>
        public TConfig Config { get; }

        /// <summary>
        /// True if the timeout timer is running.  False otherwise.
        /// </summary>
        protected bool TimeoutTimerRunning;

        /// <summary>
        /// Timer used to verify that the sessions are still connected.
        /// </summary>
        protected readonly Timer TimeoutTimer;

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

        /// <summary>
        /// Cache for commonly called methods used throughout the session.
        /// </summary>
        protected readonly ServiceMethodCache ServiceMethodCache;

        /// <summary>
        /// Base constructor to all socket classes.
        /// </summary>
        /// <param name="config">Configurations for this socket.</param>
        protected SessionHandler(TConfig config, ITransportLayer transportLayer)
        {
            TransportLayer = transportLayer;
            TimeoutTimer = new Timer(TimeoutCallback);
            ServiceMethodCache = new ServiceMethodCache();
            Config = config;

            var modeLower = TransportLayer.Mode.ToString().ToLower();

            if (TransportLayer.Mode == TransportLayerMode.Client)
            {
                OutboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
                {
                    ThreadName = $"{modeLower}-outbox",
                    StartThreads = 1
                });
                InboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
                {
                    ThreadName = $"{modeLower}-inbox",
                    StartThreads = 1
                });
            }
            else
            {
                var processorThreads = config.ProcessorThreads == -1
                    ? Environment.ProcessorCount
                    : config.ProcessorThreads;

                OutboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
                {
                    ThreadName = $"{modeLower}-outbox",
                    StartThreads = processorThreads
                });
                InboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
                {
                    ThreadName = $"{modeLower}-inbox",
                    StartThreads = processorThreads
                });
            }

            OutboxProcessor.Start();
            InboxProcessor.Start();


        }


        /// <summary>
        /// Called by the timer to verify that the session is still connected.  If it has timed out, close it.
        /// </summary>
        /// <param name="state">Concurrent dictionary of the sessions.</param>
        protected virtual void TimeoutCallback(object state)
        {
            var timoutInt = Config.PingTimeout;
            var timeoutTime = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 0, 0, timoutInt));

            foreach (var session in ConnectedSessions.Values)
            {
                if (session.LastReceived < timeoutTime)
                {
                    session.Close(SessionCloseReason.TimeOut);
                }
            }
        }


        /// <summary>
        /// Method called when a session connects.
        /// </summary>
        /// <param name="session">Session that connected.</param>
        protected virtual void OnConnect(TSession session)
        {
            // Start the timeout timer if it is not already running.
            if (TimeoutTimerRunning == false)
            {
                TimeoutTimer.Change(0, Config.PingTimeout);
                TimeoutTimerRunning = true;
            }

            Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
        }


        /// <summary>
        /// Method called when a session closes.
        /// </summary>
        /// <param name="session">Session that closed.</param>
        /// <param name="reason">Reason for the closing of the session.</param>
        protected virtual void OnClose(TSession session, SessionCloseReason reason)
        {
            // If there are no clients connected, stop the timer.
            if (ConnectedSessions.IsEmpty)
            {
                TimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                TimeoutTimerRunning = false;
            }

            Closed?.Invoke(this, new SessionClosedEventArgs<TSession, TConfig>(session, reason));
        }

        /// <summary>
        /// Method called when new sessions are created.  Override to change behavior.
        /// </summary>
        /// <returns>New session instance.</returns>
        protected virtual TSession CreateSession(ITransportLayerSession transportSession)
        {
            var session = SocketSession<TSession, TConfig>.Create(transportSession,
                Config, 
                this);

            SessionSetup?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
            session.Closed += (sender, args) => OnClose(session, args.CloseReason);

            return session;
        }

        public IEnumerator<KeyValuePair<Guid, TSession>> GetSessionsEnumerator()
        {
            return ConnectedSessions.GetEnumerator();
        }
    }
}