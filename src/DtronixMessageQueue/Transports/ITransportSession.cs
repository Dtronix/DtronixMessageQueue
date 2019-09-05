using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportSession : IDisposable
    {
        event EventHandler<TransportReceiveEventArgs> Received;
        event EventHandler<TransportSessionEventArgs> Sent;
        event EventHandler<TransportSessionEventArgs> Disconnected;
        event EventHandler<TransportSessionEventArgs> Connected;

        TransportState State { get; }

        TransportMode Mode { get; }


        void Connect();
        void Disconnect();

        bool Send(ReadOnlyMemory<byte> buffer);
        


    }
}
