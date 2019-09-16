namespace DtronixMessageQueue.Layers
{
    /// <summary>
    /// Mode that the current Socket base is in.
    /// </summary>
    public enum SessionMode
    {
        Unset,

        /// <summary>
        /// Socket base is running in server mode.
        /// </summary>
        Server,

        /// <summary>
        /// Socket base is running in client mode.
        /// </summary>
        Client
    }
}