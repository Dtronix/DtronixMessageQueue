using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.ApplicationLayers.Tls
{
    public class TlsLayerClientConnector : SocketClientConnector
    {
        public TlsLayerClientConnector(ITransportFactory factory)
         : base(factory)
        {
        }

    }
}
