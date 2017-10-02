using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public abstract class MqSessionHandler<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Mode that this socket is running as.
        /// </summary>
        public TransportLayerMode LayerMode => TransportLayer.Mode;


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

            TransportLayer.Connected += (sender, args) =>
            {
                // Convert the TransportLayerSession into a MqSession
                var session = MqSession<TSession, TConfig>.Create(args.Session, Config, this);
                session.Closed += (s, a) => Closed?.Invoke(s, a);
                args.Session.ImplementedSession = session;

                session.IncomingMessage += (s, a) => IncomingMessage?.Invoke(this, a);

                // Invoke the outer connecting event.
                Connected?.Invoke(this, new SessionEventArgs<TSession, TConfig>(session));
            };

            TransportLayer.Closed += (sender, args) =>
            {

                OutboxProcessor.Stop();
                InboxProcessor.Stop();

                Closed?.Invoke(this,
                    new SessionCloseEventArgs<TSession, TConfig>((TSession) args.Session.ImplementedSession, args.Reason));
            };



            OutboxProcessor.Start();
            InboxProcessor.Start();
        }

        public IEnumerator<KeyValuePair<Guid, TSession>> GetSessionsEnumerator()
        {
            return ConnectedSessions.GetEnumerator();
        }
    }
}