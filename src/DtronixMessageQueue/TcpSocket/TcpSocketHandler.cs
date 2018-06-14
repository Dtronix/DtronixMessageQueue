using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Base socket for all server and client sockets.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public abstract class TcpSocketHandler<TSession, TConfig>
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
        /// <summary>
        /// Mode that this socket is running as.
        /// </summary>
        public TcpSocketMode Mode { get; }

        /// <summary>
        /// True if the socket is connected/listening.
        /// </summary>
        public abstract bool IsRunning { get; }

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
        /// Main socket used by the child class for connection or for the listening of incoming connections.
        /// </summary>
        protected System.Net.Sockets.Socket MainSocket;

        /// <summary>
        /// Pool of async args for sessions to use.
        /// </summary>
        protected SocketAsyncEventArgsManager AsyncManager;

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
        /// <param name="mode">Mode of that this socket is running in.</param>
        protected TcpSocketHandler(TConfig config, TcpSocketMode mode)
        {
            TimeoutTimer = new Timer(TimeoutCallback);
            ServiceMethodCache = new ServiceMethodCache();
            Mode = mode;
            Config = config;
            var modeLower = mode.ToString().ToLower();

            if (mode == TcpSocketMode.Client)
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
                    session.Close(CloseReason.TimeOut);
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
        protected virtual void OnClose(TSession session, CloseReason reason)
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
        /// Called by the constructor of the sub-class to set all configurations.
        /// </summary>
        protected void Setup()
        {
            // Use the max connections plus one for the disconnecting of 
            // new clients when the MaxConnections has been reached.
            var maxConnections = Config.MaxConnections + 1;

            // preallocate pool of SocketAsyncEventArgs objects
            var bufferSize = Config.SendAndReceiveBufferSize / 16 * 16 + 16;

            AsyncManager = new SocketAsyncEventArgsManager(bufferSize * 2 * maxConnections,
                bufferSize);
        }

        /// <summary>
        /// Method called when new sessions are created.  Override to change behavior.
        /// </summary>
        /// <returns>New session instance.</returns>
        protected virtual TSession CreateSession(System.Net.Sockets.Socket socket)
        {
            var session = TcpSocketSession<TSession, TConfig>.Create(
                new TlsSocketSessionCreateArguments<TSession, TConfig>
                {
                    SessionSocket = socket,
                    SocketArgsManager = AsyncManager,
                    SessionConfig = Config,
                    TlsSocketHandler = this,
                    InboxProcessor = InboxProcessor,
                    OutboxProcessor = OutboxProcessor,
                    ServiceMethodCache = ServiceMethodCache
                });

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