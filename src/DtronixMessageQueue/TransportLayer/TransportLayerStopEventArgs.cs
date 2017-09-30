using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerStopEventArgs : EventArgs
    {
        public ITransportLayer TransportLayer { get; }
        public SessionCloseReason Reason { get; }

        public TransportLayerStopEventArgs(ITransportLayer transportLayer, SessionCloseReason reason)
        {
            TransportLayer = transportLayer;
            Reason = reason;
        }
    }
}
