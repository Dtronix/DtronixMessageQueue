using System;
using System.Collections.Generic;
using System.Text;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.Sockets
{
    public class SocketClientConnector : IClientConnector
    {
        protected IClientConnector Connector;
        protected TransportConfig Config;

        public Action<ISession> Connected { get; set; }
        public Action ConnectionError { get; set; }

        public ISession Session { get; private set; }



        public SocketClientConnector(ITransportFactory factory)
        {
            Connector = factory.CreateConnector(OnSessionCreated);
            Config = factory.Config;

            Connector.Connected = OnConnected;
            Connector.ConnectionError = OnConnectorConnectionError;
        }

        private void OnConnectorConnectionError()
        {
            ConnectionError?.Invoke();
        }

        protected virtual void OnSessionCreated(ISession session)
        {
            if (session is ITransportSession transportSession)
            {
                // Set the wrapper session to this new socket session.
                transportSession.WrapperSession = new SocketSession(transportSession);
            }
        }


        protected virtual void OnConnected(ISession session)
        {
            if (session is ITransportSession transportSession)
            {
                Session = transportSession.WrapperSession;
                Connected?.Invoke(transportSession.WrapperSession);
            }
        }


        public void Connect()
        {
            Connector.Connect();
        }
    }
}
