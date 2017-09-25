using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerEventArgs : EventArgs
    {
        public ITransportLayer TransportLayer { get; }

        public TransportLayerEventArgs(ITransportLayer transportLayer)
        {
            TransportLayer = transportLayer;
        }
    }
}
