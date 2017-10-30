using System;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests
{
    public class MemoryQueueStreamTests
    {
        private MemoryQueueStream _stream;
        private byte[] _expectedBytes;
        private Random Random = new Random();

        [SetUp]
        public void Init()
        {

            _stream = new MemoryQueueStream();

            _expectedBytes = new byte[128];
            Random.NextBytes(_expectedBytes);
        }

        [Test]
        public void Writer_writes_buffer()
        {
            _stream.Write(_expectedBytes);

            var readBytes = new byte[_expectedBytes.Length];
            _stream.Read(readBytes, 0, readBytes.Length);

            Assert.AreEqual(_expectedBytes, readBytes);
        }

        [Test]
        public void Writer_writes_buffer_increments()
        {
            for (int i = 0; i < _expectedBytes.Length; i++)
            {
                _stream.Write(new[] {_expectedBytes[i]}, 0, 1);
            }
            

            var readBytes = new byte[_expectedBytes.Length];
            _stream.Read(readBytes, 0, readBytes.Length);

            Assert.AreEqual(_expectedBytes, readBytes);
        }

        [Test]
        public void Writer_length_updates_on_write()
        {

            Assert.AreEqual(0, _stream.Length);
            for (int i = 0; i < _expectedBytes.Length/2; i++)
            {
                _stream.Write(new[] { _expectedBytes[i] }, 0, 1);
            }

            Assert.AreEqual(_expectedBytes.Length / 2, _stream.Length);

            for (int i = _expectedBytes.Length / 2; i < _expectedBytes.Length; i++)
            {
                _stream.Write(new[] { _expectedBytes[i] }, 0, 1);
            }

            Assert.AreEqual(_expectedBytes.Length, _stream.Length);
        }

        [Test]
        public void Writer_length_updates_on_read()
        {

            Assert.AreEqual(0, _stream.Length);
            _stream.Write(_expectedBytes);
            Assert.AreEqual(_expectedBytes.Length, _stream.Length);

            var buffer = new byte[_expectedBytes.Length / 2];
            _stream.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(buffer.Length, _stream.Length);
            _stream.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(0, _stream.Length);
        }

        [Test]
        public void Writer_copies_buffer_reference()
        {
            _stream.Write(_expectedBytes);
            Assert.AreSame(_expectedBytes, _stream.Buffer.Dequeue());
        }

        [Test]
        public void Writer_copies_buffer()
        {
            _stream.Write(_expectedBytes, 0, _expectedBytes.Length);

            Assert.AreNotSame(_expectedBytes, _stream.Buffer.Dequeue());
        }
    }
}