using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public class TransportSessionEventArgs : EventArgs
    {
        public TransportSessionEventArgs(ITransportSession session)
        {
            Session = session;
        }

        public ITransportSession Session { get; set; }
    }
}
