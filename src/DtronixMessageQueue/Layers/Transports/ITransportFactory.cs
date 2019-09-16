using System;

namespace DtronixMessageQueue.Layers.Transports
{
    public interface ITransportFactory
    {
        TransportConfig Config { get; }
        IListener CreateListener(Action<ISession> onSessionCreated);
        IClientConnector CreateConnector(Action<ISession> onSessionCreated);
    }
}
