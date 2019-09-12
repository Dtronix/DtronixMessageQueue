using System;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.Sockets.Tls
{
    public class TlsSocketClientConnector : SocketClientConnector
    {
        public TlsSocketClientConnector(ITransportFactory factory)
         : base(factory)
        {
        }

    }
}
