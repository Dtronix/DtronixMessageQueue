using System;

namespace DtronixMessageQueue.Layers.Transports
{
    public interface ITransportFactory
    {
        TransportConfig Config { get; }
        IListener CreateListener(Action<ITransportSession> onSessionCreated);
        IClientConnector CreateConnector(Action<ITransportSession> onSessionCreated);
    }
}
