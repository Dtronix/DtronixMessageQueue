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



        public IListener CreateListener(Action<ITransportSession> onSessionCreated)
        {
            return new TcpTransportListener(Config)
            {
                SessionCreated = onSessionCreated
            };
        }

        public IClientConnector CreateConnector(Action<ITransportSession> onSessionCreated)
        {
            return new TcpTransportClientConnector(Config)
            {
                SessionCreated = onSessionCreated
            };
        }
    }
}
