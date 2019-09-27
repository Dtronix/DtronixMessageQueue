using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsApplicationListener : ApplicationListener
    {
        private readonly TlsApplicationConfig _config;
        private readonly BufferMemoryPool _memoryPool;
        private readonly TlsTaskScheduler _tlsTaskScheduler;

        public TlsApplicationListener(ITransportFactory factory, TlsApplicationConfig config)
            : base(factory)
        {
            _config = config;
            _memoryPool = new BufferMemoryPool(factory.Config.SendAndReceiveBufferSize, 2 * factory.Config.MaxConnections);
            _tlsTaskScheduler = new TlsTaskScheduler(config);
        }


        protected override ApplicationSession CreateSession(ITransportSession session)
        {
            return new TlsApplicationSession(session, _config, _memoryPool, _tlsTaskScheduler);
        }
    }
}
