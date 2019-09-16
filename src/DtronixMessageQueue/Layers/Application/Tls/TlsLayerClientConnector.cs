using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsLayerClientConnector : ApplicationClientConnector
    {
        public TlsLayerClientConnector(ITransportFactory factory)
         : base(factory)
        {
        }

    }
}
