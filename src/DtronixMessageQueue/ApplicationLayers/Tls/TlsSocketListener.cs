using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.ApplicationLayers.Tls
{
    public class TlsSocketListener : SocketListener
    {
        
        public TlsSocketListener(ITransportFactory factory)
        : base(factory)
        {

        }

    }
}
