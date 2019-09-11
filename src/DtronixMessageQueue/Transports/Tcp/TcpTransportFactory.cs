using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports.Tcp
{
    public class TcpTransportFactory : ITransportFactory
    {
        public TransportConfig Config { get; }

        public TcpTransportFactory(TransportConfig config)
        {
            Config = config;
        }



        public IListener CreateListener(Action<ISession> onSessionCreated)
        {
            return new TcpTransportListener(Config)
            {
                SessionCreated = onSessionCreated
            };
        }

        public IClientConnector CreateConnector(Action<ISession> onSessionCreated)
        {
            return new TcpTransportClientConnector(Config)
            {
                SessionCreated = onSessionCreated
            };
        }
    }
}
