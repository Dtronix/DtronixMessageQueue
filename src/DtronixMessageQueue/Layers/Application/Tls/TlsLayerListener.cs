using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsLayerListener : ApplicationListener
    {
        
        public TlsLayerListener(ITransportFactory factory)
        : base(factory)
        {

        }

    }
}
