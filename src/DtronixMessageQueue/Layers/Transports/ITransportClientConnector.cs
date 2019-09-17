using System;

namespace DtronixMessageQueue.Layers.Transports
{
    interface ITransportClientConnector : IClientConnector
    {
        Action<ITransportSession> SessionCreated { get; set; }
    }
}
