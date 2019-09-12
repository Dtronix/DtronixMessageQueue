using System;

namespace DtronixMessageQueue.Sockets.Tls
{
    public class TlsSocketSession : SocketSession
    {
        public TlsSocketSession(ISession session)
        :base(session)
        {

        }

    }
}
