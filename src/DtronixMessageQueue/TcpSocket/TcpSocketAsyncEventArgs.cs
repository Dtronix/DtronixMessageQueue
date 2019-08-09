using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.TcpSocket
{
    internal class TcpSocketAsyncEventArgs : SocketAsyncEventArgs
    {
        public IMemoryOwner<byte> MemoryOwner { get; set; }

        public void Free()
        {
            MemoryOwner.Dispose();
            Dispose();
        }

    }
}
