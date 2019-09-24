using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    internal class TlsInnerStream : Stream
    {
        private readonly Action<ReadOnlyMemory<byte>> _onWrite;

        private bool _closed = false;
        //private IMemoryOwner<byte> _receiveOwner;

        //public Memory<byte> ReceiveBuffer { get; }

        private ReadOnlyMemory<byte> _received;

        public bool IsReadWaiting => _receiveSemaphore.CurrentCount == 0;

        private Mutex _receiveMutex = new Mutex();
        private SemaphoreSlim _readSemaphore = new SemaphoreSlim(0, 1);
        private SemaphoreSlim _receiveSemaphore = new SemaphoreSlim(0, 1);

        public TlsInnerStream(Action<ReadOnlyMemory<byte>> onWrite)
        {
            //_receiveOwner = receiveOwner;
            //ReceiveBuffer = receiveOwner.Memory;
            _onWrite = onWrite;
            
        }
        
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Close()
        {

        }

        public override void Flush()
        {
        }

        public override long Length => throw new NotImplementedException();

        public override long Position {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }



        public void Received(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            _receiveSemaphore.Wait(cancellationToken);

            _received = buffer;
            _receivePosition = 0;

            if (_readSemaphore.CurrentCount == 0)
                _readSemaphore.Release();
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        private int _receivePosition = 0;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            if (_receivePosition == 0)
            {
                if(_receiveSemaphore.CurrentCount == 0)
                    _receiveSemaphore.Release();

                await _readSemaphore.WaitAsync(cancellationToken);
                
            }

            if (buffer.Length >= _received.Length)
            {
                _received.CopyTo(buffer);
                _receivePosition = 0;
                return _received.Length;
            }

            var maxReadLength = Math.Min(buffer.Length, _received.Length - _receivePosition);

            _received.Slice(_receivePosition, maxReadLength).CopyTo(buffer);


            _receivePosition += maxReadLength;

            if (_receivePosition == _received.Length)
            {
                _receivePosition = 0;
            }

            return maxReadLength;
        }




        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return base.WriteAsync(buffer, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            base.Write(buffer);
        }

        public override void Write(byte[] buf, int off, int len)
        {
            _onWrite(new ReadOnlyMemory<byte>(buf, off, len));
        }

    }
}
