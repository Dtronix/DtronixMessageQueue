using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.TcpSocket
{
    public class TcpSocketAsyncEventArgs : SocketAsyncEventArgs
    {
        private IMemoryOwner<byte> _memoryOwner;

        public TcpSocketAsyncEventArgs(BufferMemoryPool argsBufferPool)
        {
            _memoryOwner = argsBufferPool.Rent();
            SetBuffer(_memoryOwner.Memory);
        }

        public void Free()
        {
            if(_memoryOwner == null)
                throw new ObjectDisposedException(nameof(TcpSocketAsyncEventArgs));

            _memoryOwner.Dispose();
            _memoryOwner = null;
            Dispose();
        }

    }
}
