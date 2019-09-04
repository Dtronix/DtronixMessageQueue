using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using DtronixMessageQueue.TcpSocket;
using DtronixMessageQueue.TlsSocket;

namespace DtronixMessageQueue.Tests.TlsSocket
{
    class TestTlsSocketSession : TlsSocketSession
    {
        public event EventHandler<ReadOnlyMemory<byte>> OnReceive;
        
        public TestTlsSocketSession(TlsSocketSessionCreateArguments args) : base(args)
        {

        }

        public TestTlsSocketSession Create(Socket socket, TcpSocketMode mode)
        {
            var modeLower = mode.ToString().ToLower();

            var outboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
            {
                ThreadName = $"{modeLower}-outbox",
                StartThreads = mode == TcpSocketMode.Client ? 1 : Environment.ProcessorCount
            });

            var inboxProcessor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
            {
                ThreadName = $"{modeLower}-inbox",
                StartThreads = mode == TcpSocketMode.Client ? 1 : Environment.ProcessorCount
            });

            return new TestTlsSocketSession(new TlsSocketSessionCreateArguments
            {
                Mode = mode,
                BufferPool = new BufferMemoryPool(Config.SendAndReceiveBufferSize, 2 * 1),
                InboxProcessor = inboxProcessor,
                OutboxProcessor = outboxProcessor,
                SessionSocket = socket
            });
        }

        protected override void HandleIncomingBytes(ReadOnlyMemory<byte> buffer)
        {
            OnReceive?.Invoke(this, buffer);
        }
    }
}
