using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.ApplicationLayers.Tls
{
    public class TlsLayerListener : SocketListener
    {
        
        public TlsLayerListener(ITransportFactory factory)
        : base(factory)
        {

        }

    }
}
