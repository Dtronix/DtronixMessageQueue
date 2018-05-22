using System;
using System.Threading;
using DtronixMessageQueue.TlsSocket;


namespace DtronixMessageQueue
{
    /// <summary>
    /// Client used to connect to a remote message queue server.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class MqClient<TSession, TConfig> : TlsSocketClient<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Event fired when a new message arrives at the mailbox.
        /// </summary>
        public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

        /// <summary>
        /// Timer used to ping the server and verify that the sessions are still connected.
        /// </summary>
        private readonly Timer _pingTimer;

        /// <summary>
        /// Initializes a new instance of a message queue.
        /// </summary>
        public MqClient(TConfig config) : base(config)
        {
            // Override the default connection limit and read/write workers.
            config.MaxConnections = 1;
            _pingTimer = new Timer(PingCallback);
            Setup();
        }

        protected override void OnConnect(TSession session)
        {
            // Start the timeout timer.
            var pingFrequency = Config.PingFrequency;

            if (pingFrequency > 0)
            {
                _pingTimer.Change(pingFrequency / 2, pingFrequency);
            }

            if (TimeoutTimerRunning == false)
            {
                TimeoutTimer.Change(0, Config.PingTimeout);
                TimeoutTimerRunning = true;
            }

            base.OnConnect(session);
        }

        protected override void OnClose(TSession session, CloseReason reason)
        {
            // Stop the timeout timer.
            _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);

            base.OnClose(session, reason);
        }


        /// <summary>
        /// Called by the ping timer to send a ping packet to the server.
        /// </summary>
        /// <param name="state">Concurrent dictionary of the sessions.</param>
        private void PingCallback(object state)
        {
            Session.Send(Session.CreateFrame(null, MqFrameType.Ping));
        }

        /// <summary>
        /// Event method invoker
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event object containing the mailbox to retrieve the message from.</param>
        private void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e)
        {
            IncomingMessage?.Invoke(sender, e);
        }

        protected override TSession CreateSession(System.Net.Sockets.Socket sessionSocket)
        {
            var session = base.CreateSession(sessionSocket);
            session.IncomingMessage += OnIncomingMessage;

            return session;
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
            if (Session == null)
            {
                return;
            }
            Session.IncomingMessage -= OnIncomingMessage;
            Session.Close(CloseReason.Closing);
            Session.Dispose();
        }

        /// <summary>
        /// Disposes of all resources associated with this client.
        /// </summary>
        public void Dispose()
        {
            _pingTimer.Dispose();
            Session.Dispose();
        }
    }
}