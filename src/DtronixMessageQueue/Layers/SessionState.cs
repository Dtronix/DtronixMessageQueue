namespace DtronixMessageQueue.Layers
{
    public enum SessionState : byte
    {
        /// <summary>
        /// State has not been set.
        /// </summary>
        Unknown,

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
