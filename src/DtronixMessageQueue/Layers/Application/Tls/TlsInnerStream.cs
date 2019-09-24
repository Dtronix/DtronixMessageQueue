using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    internal class TlsInnerStream : Stream
    {
        private readonly Action<ReadOnlyMemory<byte>> _onWrite;

        //private IMemoryOwner<byte> _receiveOwner;

        //public Memory<byte> ReceiveBuffer { get; }

        private ReadOnlyMemory<byte> _received = ReadOnlyMemory<byte>.Empty;

        public bool IsReadWaiting => _receiveSemaphore.CurrentCount == 0;

        private Mutex _receiveMutex = new Mutex();
        private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _receiveSemaphore = new SemaphoreSlim(0, 1);

        private int _receivePosition = 0;

        public TlsInnerStream(Action<ReadOnlyMemory<byte>> onWrite)
        {
            //_receiveOwner = receiveOwner;
            //ReceiveBuffer = receiveOwner.Memory;
            _onWrite = onWrite;
        }

        public bool AsyncMode { get; set; } = true;
        
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;


        public override long Length => throw new NotImplementedException();

        public override long Position {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }



        /// <summary>
        /// Method to provide buffer data to 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        public void AsyncReadReceived(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if(!AsyncMode)
                throw new InvalidOperationException("Stream in synchronous mode.  Can not use async methods.");

            _receiveSemaphore.Wait(cancellationToken);

            _received = buffer;
            _receivePosition = 0;

            if (_readSemaphore.CurrentCount == 0)
                _readSemaphore.Release();
            
        }


        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            if (!AsyncMode)
                throw new InvalidOperationException("Stream in synchronous mode.  Can not use async methods.");

            if (_received.IsEmpty)
            {
                if (_receiveSemaphore.CurrentCount == 0)
                    _receiveSemaphore.Release();

                await _readSemaphore.WaitAsync(cancellationToken);
            }

            return CopyDataToBuffer(buffer.Span);
        }

        public override int Read(Span<byte> buffer)
        {
            if (AsyncMode)
                throw new InvalidOperationException("Stream in async mode.  Can not use sync methods.");


            if (_received.IsEmpty)
            {
                if (_receiveSemaphore.CurrentCount == 0)
                    _receiveSemaphore.Release();

                _readSemaphore.Wait();
            }

            return CopyDataToBuffer(buffer);
        }


        private int CopyDataToBuffer(Span<byte> buffer)
        {
            int readLength = 0;
            if (buffer.Length >= _received.Length)
            {
                _received.Span.CopyTo(buffer);
                _receivePosition = 0;
                readLength = _received.Length;
                _received = ReadOnlyMemory<byte>.Empty;
            }
            else
            {
                readLength = Math.Min(buffer.Length, _received.Length - _receivePosition);

                _received.Slice(_receivePosition, readLength).Span.CopyTo(buffer);

                _receivePosition += readLength;

                if (_receivePosition == _received.Length)
                {
                    _receivePosition = 0;
                    _received = ReadOnlyMemory<byte>.Empty;
                }
            }

            return readLength;
        }

        public override void Close()
        {

        }

        public override void Flush()
        {
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
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
