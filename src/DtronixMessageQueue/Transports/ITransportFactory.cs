using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportFactory
    {
        TransportConfig Config { get; }
        IListener CreateListener(Action<ISession> onSessionCreated);
        IClientConnector CreateConnector(Action<ISession> onSessionCreated);
    }
}
