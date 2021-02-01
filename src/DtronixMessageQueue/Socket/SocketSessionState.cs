namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Current state of the socket.
    /// </summary>
    public enum SocketSessionState : byte
    {
        /// <summary>
        /// State has not been set.
        /// </summary>
        Unknown,

        /// <summary>
        /// Session is in the process of securing the communication channel.
        /// </summary>
        Securing,

        /// <summary>
        /// Session has connected to remote session.
        /// </summary>
        Connected,

        /// <summary>
        /// Session has been closed and no longer can be used.
        /// </summary>
        Closed,

        /// <summary>
        /// Socket is in an error state.
        /// </summary>
        Error,
    }
}