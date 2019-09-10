using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportSession
    {
        Action<ReadOnlyMemory<byte>> Received { get; set; }
        Action<ITransportSession> Sent { get; set; }
        event EventHandler<TransportSessionEventArgs> Disconnected;
        event EventHandler<TransportSessionEventArgs> Connected;

        TransportState State { get; }


        void Connect();
        void Disconnect();

        bool Send(ReadOnlyMemory<byte> buffer);
    }
}
