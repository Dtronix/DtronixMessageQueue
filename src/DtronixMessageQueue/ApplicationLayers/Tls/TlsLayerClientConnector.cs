using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.ApplicationLayers.Tls
{
    public class TlsSocketClientConnector : SocketClientConnector
    {
        public TlsSocketClientConnector(ITransportFactory factory)
         : base(factory)
        {
        }

    }
}
