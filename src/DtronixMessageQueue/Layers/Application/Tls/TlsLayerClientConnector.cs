using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsLayerClientConnector : ApplicationClientConnector
    {
        private readonly TlsLayerConfig _config;
        private readonly BufferMemoryPool _memoryPool;
        private readonly TlsAuthScheduler _tlsAuthScheduler;

        public TlsLayerClientConnector(ITransportFactory factory, TlsLayerConfig config)
         : base(factory)
        {
            _config = config;
            _memoryPool = new BufferMemoryPool(factory.Config.SendAndReceiveBufferSize, 2 * factory.Config.MaxConnections);
            _tlsAuthScheduler = new TlsAuthScheduler();
        }

        protected override void OnSessionCreated(ISession session)
        {
            if (session is ITransportSession transportSession)
            {
                // Set the wrapper session to this new socket session.
                transportSession.WrapperSession
                    = new TlsLayerSession(transportSession, _config, _memoryPool, _tlsAuthScheduler);
            }
        }

    }
}
