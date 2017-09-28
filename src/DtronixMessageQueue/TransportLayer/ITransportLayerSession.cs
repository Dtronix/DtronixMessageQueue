using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public interface ITransportLayerSession
    {
        Guid Id { get; }
        TransportLayerState State { get; }

        /// <summary>
        /// Last time the session received anything from the socket.
        /// </summary>
        DateTime LastReceived { get; }

        /// <summary>
        /// Time that this session connected to the server.
        /// </summary>
        DateTime ConnectedTime { get; }

        event EventHandler<TransportLayerSessionEventArgs> Connecting;
        event EventHandler<TransportLayerSessionEventArgs> Connected;

        event EventHandler<TransportLayerSessionCloseEventArgs> Closing;
        event EventHandler<TransportLayerSessionCloseEventArgs> Closed;

        event EventHandler<byte[]> Received; 

        void Send(byte[] buffer, int start, int count);

        void Receieve();

        void Close(SessionCloseReason reason);
    }
}
