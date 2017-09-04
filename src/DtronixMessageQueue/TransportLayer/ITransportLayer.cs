using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.TransportLayer
{
    public interface ITransportLayer
    {
        bool IsRunning { get; }
        TransportLayerMode Mode { get; }
        TransportLayerState State { get; }
        void Send(byte[] buffer, int start, int count);
        void Listen();
        void Close();
        void Connect();
        void Disconnect();
        void AcceptConnection();
    }
}
