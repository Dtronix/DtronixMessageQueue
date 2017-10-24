using System;
using System.Threading;
using System.Threading.Tasks;
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

        private CancellationTokenSource _cancellationTokenSource;

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
        public new void Dispose()
        {
            Session.Close(SessionCloseReason.Closing);

            base.Dispose();
        }

        public void Connect()
        {
            Connect(CancellationToken.None);
        }

        /// <summary>
        /// Connects to the configured endpoint.
        /// </summary>
        public void Connect(CancellationToken token)
        {
            TransportLayer.Connect();

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Config.ConnectionTimeout, token);
                }
                catch
                {
                    return;
                }

                if (TransportLayer.State != TransportLayerState.Connected)
                {
                    TransportLayer.Close();
                    OnClose(new SessionCloseEventArgs<TSession, TConfig>(null, SessionCloseReason.TimeOut));
                }

            });
        }


        protected void Close(SessionCloseReason reason)
        {
            Session.Close(reason);
        }
    }
}