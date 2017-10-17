using System;
using System.Threading;
using DtronixMessageQueue.TransportLayer;


namespace DtronixMessageQueue
{
    /// <summary>
    /// Client used to connect to a remote message queue server.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class MqClient<TSession, TConfig> : MqSessionHandler<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        private bool _isPingTimerRunning;
        private readonly Timer _pingTimer;

        /// <summary>
        /// Session for this client.
        /// </summary>
        public TSession Session { get; private set; }

        /// <summary>
        /// Initializes a new instance of a message queue.
        /// </summary>
        public MqClient(TConfig config) : base(config, TransportLayerMode.Client)
        {
            _pingTimer = new Timer(OnPingTimer);
        }

        protected override void OnConnected(TSession session)
        {
            Session = session;

            var pingFrequency = Config.PingFrequency;

            if (pingFrequency > 0 && !_isPingTimerRunning)
            {
                _isPingTimerRunning = true;
                _pingTimer.Change(pingFrequency, pingFrequency);
            }

            base.OnConnected(session);
        }

        protected virtual void OnPingTimer(object state)
        {
            // Ping the server.
            Session.Send(Session.CreateFrame(null, MqFrameType.Ping));
        }


        /// <summary>
        /// Adds a frame to the outbox to be processed.
        /// </summary>
        /// <param name="frame">Frame to send.</param>
        public void Send(MqFrame frame)
        {
            Send(new MqMessage(frame));
        }

        /// <summary>
        /// Adds a message to the outbox to be processed.
        /// Empty messages will be ignored.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void Send(MqMessage message)
        {
            // Send the outgoing message to the session to be processed by the postmaster.
            Session.Send(message);
        }

        public void Close()
        {
            Session?.Close(SessionCloseReason.Closing);
        }

        /// <summary>
        /// Disposes of all resources associated with this client.
        /// </summary>
        public void Dispose()
        {
            Session.Close(SessionCloseReason.Closing);
        }



        /// <summary>
        /// Connects to the configured endpoint.
        /// </summary>
        public void Connect()
        {
            TransportLayer.Connect();
        }


        protected void Close(SessionCloseReason reason)
        {
            TransportLayer.Close(reason);
        }
    }
}