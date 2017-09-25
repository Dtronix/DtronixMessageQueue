namespace DtronixMessageQueue.TransportLayer
{
    /// <summary>
    /// Mode that the current Socket base is in.
    /// </summary>
    public enum TransportLayerMode
    {
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