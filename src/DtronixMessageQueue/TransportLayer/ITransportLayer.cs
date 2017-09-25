using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.TransportLayer
{
    public interface ITransportLayer
    {


        event EventHandler<TransportLayerEventArgs> Starting;
        event EventHandler<TransportLayerEventArgs> Started;

        event EventHandler<TransportLayerStopEventArgs> Stopping;
        event EventHandler<TransportLayerStopEventArgs> Stopped;

        event EventHandler<TransportLayerSessionEventArgs> Connecting;
        event EventHandler<TransportLayerSessionEventArgs> Connected;

        event EventHandler<TransportLayerSessionCloseEventArgs> Closing;
        event EventHandler<TransportLayerSessionCloseEventArgs> Closed;


        event EventHandler<TransportLayerAcceptSessionEventArgs> AcceptedSession;

        TransportLayerConfig Config { get; }
        TransportLayerMode Mode { get; }
        TransportLayerState State { get; }
        ConcurrentDictionary<Guid, ITransportLayerSession> ConnectedSessions { get; }

        void Start();

        void Stop();

        void AcceptSession();

        void Connect();

        void Close();
    }
}
