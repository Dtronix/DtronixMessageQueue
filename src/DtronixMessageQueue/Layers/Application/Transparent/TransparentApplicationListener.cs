using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Transparent
{
    public class TransparentApplicationListener : ApplicationListener
    {
        private readonly ApplicationConfig _config;

        public TransparentApplicationListener(ITransportFactory factory, ApplicationConfig config)
            : base(factory)
        {
            _config = config;
        }


        protected override ApplicationSession CreateSession(ITransportSession session)
        {
            return new TransparentApplicationSession(session, _config);
        }
    }
}
