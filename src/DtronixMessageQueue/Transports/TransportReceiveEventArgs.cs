using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public class TransportReceiveEventArgs : EventArgs
    {
        public TransportReceiveEventArgs(Memory<byte> buffer)
        {
            Buffer = buffer;
        }

        public Memory<byte> Buffer { get; set; }
    }
}
