using DtronixMessageQueue.TcpSocket;

namespace DtronixMessageQueue.TlsSocket
{
    /// <summary>
    /// Event args used when a session is closed.
    /// </summary>
    public class TlsSocketSessionClosedEventArgs
    {
        /// <summary>
        /// Closed session.
        /// </summary>
        public TlsSocketSession Session { get; }

        /// <summary>
        /// Reason the session was closed.
        /// </summary>
        public CloseReason CloseReason { get; }

        /// <summary>
        /// Creates a new instance of the session closed event args.
        /// </summary>
        /// <param name="session">Closed session.</param>
        /// <param name="closeReason">Reason the session was closed.</param>
        public TlsSocketSessionClosedEventArgs(TlsSocketSession session, CloseReason closeReason)
        {
            Session = session;
            CloseReason = closeReason;
        }
    }
}