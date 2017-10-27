namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Class to implement on classes which have setup events.
    /// </summary>
    public interface ISetupSocketSession
    {
        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void Start();
    }
}