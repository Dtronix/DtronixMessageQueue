using System;

namespace DtronixMessageQueue.Layers.Transports
{
    interface ITransportClientConnector : IClientConnector
    {
        Action<ISession> SessionCreated { get; set; }
    }
}
