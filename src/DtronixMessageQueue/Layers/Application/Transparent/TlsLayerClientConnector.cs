using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Transparent
{
    public class TransparentLayerClientConnector : ApplicationClientConnector
    {
        private readonly ApplicationConfig _config;

        public TransparentLayerClientConnector(ITransportFactory factory, ApplicationConfig config)
         : base(factory)
        {
            _config = config;

        }

        protected override ApplicationSession CreateSession(ITransportSession session)
        {
            return new TransparentLayerSession(session, _config);
        }
    }
}
