using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsLayerListener : ApplicationListener
    {
        private readonly TlsLayerConfig _config;
        private readonly BufferMemoryPool _memoryPool;
        private readonly TlsAuthScheduler _tlsAuthScheduler;

        public TlsLayerListener(ITransportFactory factory, TlsLayerConfig config)
            : base(factory)
        {
            _config = config;
            _memoryPool = new BufferMemoryPool(factory.Config.SendAndReceiveBufferSize, 2 * factory.Config.MaxConnections);
            _tlsAuthScheduler = new TlsAuthScheduler();
        }


        protected override ApplicationSession CreateSession(ITransportSession session)
        {
            return new TlsLayerSession(session, _config, _memoryPool, _tlsAuthScheduler);
        }
    }
}
