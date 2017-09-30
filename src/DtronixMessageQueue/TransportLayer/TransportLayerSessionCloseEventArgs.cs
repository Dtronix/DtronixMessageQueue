using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerSessionCloseEventArgs : EventArgs
    {
        public ITransportLayerSession Session { get; }
        public SessionCloseReason Reason { get; }

        public TransportLayerSessionCloseEventArgs(ITransportLayerSession session, SessionCloseReason reason)
        {
            Session = session;
            Reason = reason;
        }
    }
}
