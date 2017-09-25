using System;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerSessionReceiveEventArgs : EventArgs
    {
        public ITransportLayerSession Session { get; }
        public byte[] Buffer { get; }

        public TransportLayerSessionReceiveEventArgs(ITransportLayerSession session, byte[] buffer)
        {
            Session = session;
            Buffer = buffer;
        }
    }
}