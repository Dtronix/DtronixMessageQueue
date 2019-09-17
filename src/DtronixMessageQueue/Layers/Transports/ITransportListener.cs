using System;

namespace DtronixMessageQueue.Layers.Transports
{
    public interface ITransportListener : IListener
    {
        Action<ITransportSession> SessionCreated { get; set; }
    }
}
