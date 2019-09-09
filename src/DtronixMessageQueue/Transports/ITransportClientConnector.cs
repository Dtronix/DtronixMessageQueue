using System;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportClientConnector
    {
        event EventHandler<TransportSessionEventArgs> Connected;
        event EventHandler ConnectionError;

        bool IsConnected { get; }

        void Connect();
    }
}