using System;

namespace DtronixMessageQueue.Layers
{
    /// <summary>
    /// Event args used when a session is closed.
    /// </summary>
    public class SessionClosedEventArgs : EventArgs
    {
        /// <summary>
        /// Closed session.
        /// </summary>
        public ISession Session { get; }

        /// <summary>
        /// Reason the session was closed.
        /// </summary>
        public CloseReason CloseReason { get; }

        /// <summary>
        /// Creates a new instance of the session closed event args.
        /// </summary>
        /// <param name="session">Closed session.</param>
        /// <param name="closeReason">Reason the session was closed.</param>
        public SessionClosedEventArgs(ISession session, CloseReason closeReason)
        {
            Session = session;
            CloseReason = closeReason;
        }
    }
}