using System;

namespace DtronixMessageQueue.Layers.Transports.Tcp
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
