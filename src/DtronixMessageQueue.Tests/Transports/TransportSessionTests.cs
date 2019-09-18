using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Transports
{
    class TransportSessionTests : TransportTestBase
    {

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void SessionSendsDataAndPeerReceives(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            listener.Connected += (o, e) =>
            {
                e.Session.Ready += (sender, args) => e.Session.Send(memory, true);
            };
            connector.Connected += (o, e) =>
            {
                e.Session.Received = buffer =>
                {
                    TestComplete.Set();
                };
                e.Session.Ready += (sender, args) => { };
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void SessionSendsDataAndPeerReceivesBeforeDisconnect(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            int totalReceived = 0;
            listener.Connected += (o, e) =>
            {
                e.Session.Send(memory, true); 
                e.Session.Disconnect();
            };
            connector.Connected += (o, e) =>
            {
                e.Session.Received = buffer =>
                {
                    totalReceived += buffer.Length;
                };
                e.Session.Disconnected += (o, eventArgs) =>
                {
                    if (totalReceived == 10)
                        TestComplete.Set();
                    else
                        LastException = new Exception("Did not receive all data from peer before disconnect.");
                };
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void SessionSendsDataAndPeerReceivesFragmented(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            listener.Connected += (o, e) =>
            {
                Task.Run(async () =>
                {
                    e.Session.Send(memory.Slice(0, 5), true);
                    await Task.Delay(50);
                    e.Session.Send(memory.Slice(5, 5), true);
                });
            };
            int totalReceived = 0;
            connector.Connected += (o, e) =>
            {
                e.Session.Received = buffer =>
                {
                    totalReceived += buffer.Length;

                    if (totalReceived == 10)
                        TestComplete.Set();
                };
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete(500);
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        public void SessionThrowsOnTooLargeSend(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[ClientConfig.SendAndReceiveBufferSize + 1]);

            listener.Connected += (o, e) =>
            {
                try
                {
                    e.Session.Send(memory, true);
                    LastException = new Exception("Did not throw on buffer overflow");
                }
                catch
                {
                    TestComplete.Set();
                }
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete(500);
        }
    }
}
