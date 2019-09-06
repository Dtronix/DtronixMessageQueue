using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;

namespace DtronixMessageQueue
{
    public class BufferMemoryPool : MemoryPool<byte>
    {

        private readonly int _totalRentals;

        /// <summary>
        ///  The underlying byte array maintained by the Buffer Manager
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        /// Stack containing the index of the freed buffers.
        /// </summary>
        private readonly ConcurrentStack<int> _freeIndexPool = new ConcurrentStack<int>();

        private readonly ConcurrentDictionary<int, BufferMemoryOwner> _rentedBuffers
            = new ConcurrentDictionary<int, BufferMemoryOwner>();

        private bool _disposed;

        /// <summary>
        /// Current index in the buffer pool
        /// </summary>
        private int _rentalIndex = 0;

        /// <summary>
        /// Current index in the buffer pool
        /// </summary>
        public int CurrentRentals => _rentalIndex;

        public int TotalRentals => _totalRentals;

        /// <summary>
        /// Size of each session buffer.
        /// </summary>
        private readonly int _rentBufferSize;

        public override int MaxBufferSize => _rentBufferSize;




        public BufferMemoryPool(int rentBufferSize, int totalRentals)
        {
            _buffer = new byte[rentBufferSize * totalRentals];
            _rentBufferSize = rentBufferSize;
            _totalRentals = totalRentals;
        }


        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            if (minBufferSize != -1 && minBufferSize != _rentBufferSize)
                throw new ArgumentOutOfRangeException(nameof(minBufferSize),
                    $"Rent size [{minBufferSize}] must either be -1 or match constructed rental size of [{_rentBufferSize}]");

            if (_disposed)
                throw new ObjectDisposedException(nameof(BufferMemoryPool));

            int index;
            if (_freeIndexPool.TryPop(out var poolIndex))
            {
                index = poolIndex;
            }
            else
            {
                if (_rentalIndex + 1 > _totalRentals)
                    throw new Exception("Pool exhausted.");

                index = Interlocked.Increment(ref _rentalIndex);
            }

            var bmo = new BufferMemoryOwner(this, index);

            _rentedBuffers.TryAdd(index, bmo);

            return bmo;
        }



        protected override void Dispose(bool disposing)
        {
            _disposed = true;

            var buffers = _rentedBuffers.ToArray();

            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i].Value.Dispose();
            }

            _buffer = null;
        }

        private sealed class BufferMemoryOwner : IMemoryOwner<byte>
        {
            private readonly BufferMemoryPool _pool;
            private readonly int _poolIndex;
            private Memory<byte> _memory;
            private bool _disposed = false;

            public Memory<byte> Memory {
                get {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(BufferMemoryOwner));

                    return _memory;
                }
            }
            public BufferMemoryOwner(BufferMemoryPool pool, int poolIndex)
            {
                _pool = pool;
                _poolIndex = poolIndex;
                _memory = new Memory<byte>(pool._buffer, poolIndex * pool._rentBufferSize, pool._rentBufferSize);
            }

            public void Dispose()
            {
                _disposed = true;
                _memory.Span.Clear();

                _pool._freeIndexPool.Push(_poolIndex);
                _pool._rentedBuffers.TryRemove(_poolIndex, out _);

                _memory = Memory<byte>.Empty;
            }
        }
    }
}
