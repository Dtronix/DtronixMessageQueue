using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerStateChangedEventArgs : EventArgs
    {
        public TransportLayerState State { get; }
        public ITransportLayerSession Session { get; }
        public ITransportLayer TransportLayer { get; }
        public SessionCloseReason Reason { get; set; }

        public TransportLayerStateChangedEventArgs(ITransportLayer transportLayer, TransportLayerState state, ITransportLayerSession session)
        {
            TransportLayer = transportLayer;
            Session = session;
            State = state;
        }

        public TransportLayerStateChangedEventArgs(ITransportLayer transportLayer, TransportLayerState state)
        {
            TransportLayer = transportLayer;
            State = state;
        }
    }
}
