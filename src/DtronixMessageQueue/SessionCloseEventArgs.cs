using System;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Event args used when a session is closed.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class SessionCloseEventArgs<TSession, TConfig> : EventArgs
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        private TSession implementedSession;
        private SessionCloseReason reason;

        /// <summary>
        /// Closed session.
        /// </summary>
        public TSession Session { get; }

        /// <summary>
        /// Reason the session was closed.
        /// </summary>
        public SessionCloseReason CloseReason { get; }

        /// <summary>
        /// Creates a new instance of the session closed event args.
        /// </summary>
        /// <param name="session">Closed session.</param>
        /// <param name="closeReason">Reason the session was closed.</param>
        public SessionCloseEventArgs(TSession session, SessionCloseReason closeReason)
        {
            Session = session;
            CloseReason = closeReason;
        }
    }
}