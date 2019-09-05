using System;
using System.Collections.Generic;
using System.Text;
using DtronixMessageQueue.TcpSocket;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportSession : IDisposable
    {
        event EventHandler<TransportReceiveEventArgs> Received;
        event EventHandler Sent;
        event EventHandler Closed;

        TransportState State { get; }

        TransportMode Mode { get; }


        void Start();

        bool Send(ReadOnlyMemory<byte> buffer);
        void Close();


    }
}
