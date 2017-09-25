using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerSessionEventArgs : EventArgs
    {
        public ITransportLayerSession Session { get; }

        public TransportLayerSessionEventArgs(ITransportLayerSession session)
        {
            Session = session;
        }
    }
}
