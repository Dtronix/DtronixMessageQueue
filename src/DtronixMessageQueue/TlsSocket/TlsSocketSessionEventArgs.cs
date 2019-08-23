namespace DtronixMessageQueue.TlsSocket
{
    /// <summary>
    /// Event args used when the session has connected to a remote endpoint.
    /// </summary>
    public class TlsSocketSessionEventArgs
    {
        /// <summary>
        /// Connected session.
        /// </summary>
        public TlsSocketSession Session { get; }

        /// <summary>
        /// Creates a new instance of the session connected event args.
        /// </summary>
        /// <param name="session">Connected session.</param>
        public TlsSocketSessionEventArgs(TlsSocketSession session)
        {
            Session = session;
        }
    }
}