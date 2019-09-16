namespace DtronixMessageQueue.Layers.Transports
{
    public interface ITransportSession : ISession
    {
        void Connect();

        /// <summary>
        /// Contains the session which is 
        /// </summary>
        ISession WrapperSession { get; set; }
    }
}
