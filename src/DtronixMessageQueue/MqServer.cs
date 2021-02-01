using System;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Message queue server to handle incoming clients
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class MqServer<TSession, TConfig> : SocketServer<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Event fired when a new message arrives at a session's mailbox.
        /// </summary>
        public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

        /// <summary>
        /// Initializes a new instance of a message queue.
        /// </summary>
        public MqServer(TConfig config) : base(config)
        {
            Setup();
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

        /// <summary>
        /// Creates a session with the specified socket.
        /// </summary>
        /// <param name="sessionSocket">Socket to associate with the session.</param>
        protected override TSession CreateSession(System.Net.Sockets.Socket sessionSocket)
        {
            var session = base.CreateSession(sessionSocket);
            session.IncomingMessage += OnIncomingMessage;

            return session;
        }

        /// <summary>
        /// Called internally with a session on the server closes.
        /// </summary>
        /// <param name="session">Session which closed.</param>
        /// <param name="reason">Reason the session closed.</param>
        protected override void OnClose(TSession session, CloseReason reason)
        {
            session.IncomingMessage -= OnIncomingMessage;
            session.Dispose();
            base.OnClose(session, reason);
        }
    }
}