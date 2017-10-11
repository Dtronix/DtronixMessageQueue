using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public interface ITransportLayer
    {
        event EventHandler<TransportLayerStateChangedEventArgs> StateChanged;

        TransportLayerConfig Config { get; }
        TransportLayerMode Mode { get; }
        TransportLayerState State { get; }
        ConcurrentDictionary<Guid, ITransportLayerSession> ConnectedSessions { get; }

        void Start();

        void Stop();

        void AcceptAsync();

        void Connect();

        void Close(SessionCloseReason reason);
    }
}
