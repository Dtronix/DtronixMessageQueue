using System;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.Sockets.Tls
{
    public class TlsSocketListener : SocketListener
    {
        
        public TlsSocketListener(ITransportFactory factory)
        : base(factory)
        {

        }

    }
}
