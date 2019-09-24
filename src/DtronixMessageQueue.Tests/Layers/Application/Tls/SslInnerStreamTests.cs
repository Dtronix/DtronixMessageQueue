using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Layers.Application.Tls;
using NUnit.Framework;


namespace DtronixMessageQueue.Tests.Layers.Application.Tls
{
    public class SslInnerStreamTests
    {
        private TlsInnerStream _innerStream;
        private Action<ReadOnlyMemory<byte>> _onWrite;
        private ReadOnlyMemory<byte> _data = new ReadOnlyMemory<byte>(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});
        private CancellationTokenSource _cancellationTokenSource;

        public SslInnerStreamTests()
        {
        }

        [SetUp]
        public void Setup()
        {
            _cancellationTokenSource = new CancellationTokenSource(100000);
            _innerStream = new TlsInnerStream(memory => _onWrite(memory));
        }

        [Test]
        public void AsyncReadBlocks()
        {
            _cancellationTokenSource = new CancellationTokenSource(100);
            var buffer = new Memory<byte>(new byte[20]);

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _innerStream.ReadAsync(buffer, _cancellationTokenSource.Token);
            });
        }

        [Test]
        public void AsyncReadBlocksUntilReceive()
        {
            var buffer = new Memory<byte>(new byte[20]);

            var task = Task.Run(async () =>
                    await _innerStream.ReadAsync(buffer, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token);

            _innerStream.Received(_data, _cancellationTokenSource.Token);

            task.Wait();

            var len = task.Result;

            Assert.IsTrue(buffer.Span.Slice(0, len).SequenceEqual(_data.Span));
        }

        [Test]
        public void AsyncReadsWithSmallBuffer()
        {
            var buffer = new Memory<byte>(new byte[1]);

            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var len = await _innerStream.ReadAsync(buffer, _cancellationTokenSource.Token);
                    Assert.AreEqual(_data.Span[i], buffer.Span[0]);
                }
            },_cancellationTokenSource.Token);
            _innerStream.Received(_data, _cancellationTokenSource.Token);
            task.Wait();
        }

        [Test]
        public void AsyncReadsWithSmallReceivedData()
        {
            var buffer = new Memory<byte>(new byte[20]);
            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var len = await _innerStream.ReadAsync(buffer, _cancellationTokenSource.Token);
                    Assert.AreEqual(_data.Span[i], buffer.Span[0]);
                }
            }, _cancellationTokenSource.Token);

            for (int i = 0; i < 10; i++)
            {
                _innerStream.Received(_data.Slice(i, 1), _cancellationTokenSource.Token);
            }
            
            task.Wait();
        }

    }
}