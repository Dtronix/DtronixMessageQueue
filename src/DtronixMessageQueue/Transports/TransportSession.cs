using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public abstract class TransportSession
    {
        public abstract event EventHandler<TransportReceiveEventArgs> Received;
        public abstract event EventHandler<TransportSessionEventArgs> Sent;
        public abstract event EventHandler<TransportSessionEventArgs> Disconnected;
        public abstract event EventHandler<TransportSessionEventArgs> Connected;

        public abstract TransportState State { get; protected set; }

        public abstract TransportMode Mode { get; protected set; }


        public abstract void Connect();
        public abstract void Disconnect();

        public abstract bool Send(ReadOnlyMemory<byte> buffer);
        protected abstract void OnReceived(SocketAsyncEventArgs e);
    }
}
