namespace DtronixMessageQueue.Layers.Transports
{
    public interface ITransportSession : ISession
    {
        void Connect();
    }
}
