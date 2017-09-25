using System;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerCloseEventArgs : EventArgs
    {
        public ITransportLayer TransportLayer { get; }
        public SessionCloseReason CloseReason { get; }

        public TransportLayerCloseEventArgs(ITransportLayer transportLayer, SessionCloseReason closeReason)
        {
            TransportLayer = transportLayer;
            CloseReason = closeReason;
        }
    }
}