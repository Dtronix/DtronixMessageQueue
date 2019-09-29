using System;
using System.Diagnostics.CodeAnalysis;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application
{
    public abstract class ApplicationClientConnector : IClientConnector
    {
        protected IClientConnector Connector;

        public event EventHandler<SessionEventArgs> Connected;
        public event EventHandler ConnectionError;


        public ISession Session { get; private set; }



        protected ApplicationClientConnector(ITransportFactory factory)
        {
            Connector = factory.CreateConnector(OnSessionCreated);

            Connector.ConnectionError += OnConnectorConnectionError;
        }

        private void OnConnectorConnectionError(object sender, EventArgs e)
        {
            ConnectionError?.Invoke(this, EventArgs.Empty);
        }

        protected abstract ApplicationSession CreateSession([NotNull] ITransportSession session);

        private void OnSessionCreated([NotNull] ITransportSession session)
        {
            var appSession = CreateSession(session);

            appSession.Connected += OnConnected;
        }

        protected virtual void OnConnected(object sender, SessionEventArgs e)
        {
            Session = e.Session;
            Connected?.Invoke(this, e);
        }


        public void Connect()
        {
            Connector.Connect();
        }
    }
}
