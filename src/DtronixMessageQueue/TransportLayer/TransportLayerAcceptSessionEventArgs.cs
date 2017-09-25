using System;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerAcceptSessionEventArgs : EventArgs
    {
        public ITransportLayer TransportLayer { get; }
        public ITransportLayerSession Session { get; }

        public TransportLayerAcceptSessionEventArgs(ITransportLayer transportLayer, ITransportLayerSession session)
        {
            TransportLayer = transportLayer;
            Session = session;
        }
    }
}