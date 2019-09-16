using System;

namespace DtronixMessageQueue.Layers.Transports
{
    public interface ITransportListener : IListener
    {
        Action<ISession> SessionCreated { get; set; }

    }
}
