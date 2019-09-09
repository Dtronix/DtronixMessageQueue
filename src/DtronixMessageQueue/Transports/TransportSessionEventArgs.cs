using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public class TransportSessionEventArgs : EventArgs
    {
        public TransportSessionEventArgs(TransportSession session)
        {
            Session = session;
        }

        public TransportSession Session { get; set; }
    }
}
