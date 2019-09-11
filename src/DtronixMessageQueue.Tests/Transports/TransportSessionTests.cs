using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Transports
{
    class TransportSessionTests : TransportTestBase
    {

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void SessionSendsDataAndPeerReceives(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            listener.Connected = session => { session.Send(memory); };
            connector.Connected = session =>
            {
                session.Received = buffer =>
                {
                    TestComplete.Set();
                };
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void SessionSendsDataAndPeerReceivesBeforeDisconnect(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            int totalReceived = 0;
            listener.Connected = session =>
            {
                session.Send(memory); 
                session.Disconnect();
            };
            connector.Connected = session =>
            {
                session.Received = buffer =>
                {
                    totalReceived += buffer.Length;
                };
                session.Disconnected += (o, eventArgs) =>
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

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void SessionSendsDataAndPeerReceivesFragmented(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            listener.Connected = session =>
            {
                Task.Run(async () =>
                {
                    session.Send(memory.Slice(0, 5));
                    await Task.Delay(50);
                    session.Send(memory.Slice(5, 5));
                });
            };
            int totalReceived = 0;
            connector.Connected = session =>
            {
                session.Received = buffer =>
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

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void SessionThrowsOnTooLargeSend(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            var memory = new Memory<byte>(new byte[ClientConfig.SendAndReceiveBufferSize + 1]);

            listener.Connected = session =>
            {
                try
                {
                    session.Send(memory);
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
