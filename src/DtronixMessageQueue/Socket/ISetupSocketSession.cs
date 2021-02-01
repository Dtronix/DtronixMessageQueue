namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Class to implement on classes which have setup events.
    /// </summary>
    public interface ISetupSocketSession
    {
        /// <summary>
        /// Start the session.
        /// </summary>
        void StartSession();
    }
}