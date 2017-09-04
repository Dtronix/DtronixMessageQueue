namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Class to implement on classes which have setup events.
    /// </summary>
    public interface ISocketSession
    {
        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void Start();
    }
}