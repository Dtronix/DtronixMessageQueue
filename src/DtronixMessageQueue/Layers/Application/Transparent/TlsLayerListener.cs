using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Transparent
{
    public class TransparentLayerListener : ApplicationListener
    {
        private readonly ApplicationConfig _config;

        public TransparentLayerListener(ITransportFactory factory, ApplicationConfig config)
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
