using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        /// <summary>
        /// Contains a reference to the session that has implemented this TransportLayerSession.
        /// </summary>
        object ImplementedSession { get; set; }

        event EventHandler<TransportLayerStateChangedEventArgs> StateChanged;

        event EventHandler<TransportLayerReceiveAsyncEventArgs> Received;

        void Send(byte[] buffer, int start, int count);

        void ReceiveAsync();

        void Close();
    }
}
