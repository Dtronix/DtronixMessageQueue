using System;
using System.Collections.Generic;
using System.Text;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.Sockets
{
    public class SocketSession
    {
        protected readonly ITransportSession Session;

        public SocketSession(ITransportSession session)
        {
            Session = session;
            Session.Received = OnReceive;
            Session.Sent = OnSent;
        }

        public virtual void Send(ReadOnlyMemory<byte> buffer)
        {
            Session.Send(buffer);
        }

        protected virtual void OnSent(ITransportSession obj)
        {
        }

        protected virtual void OnReceive(ReadOnlyMemory<byte> buffer)
        {

        }
    }
}
