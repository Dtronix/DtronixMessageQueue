using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Transports
{
    class TransportSessionTests : TransportTestBase
    {

        [Test]
        public void SessionSendsDataAndPeerReceives()
        {
            var (listener, connector) = CreateClientServer();
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            listener.Connected += (sender, args) => { args.Session.Send(memory); };
            connector.Connected += (sender, args) =>
            {
                args.Session.Received += (o, eventArgs) =>
                {
                    TestComplete.Set();
                };
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [Test]
        public void SessionSendsDataAndPeerReceivesBeforeDisconnect()
        {
            var (listener, connector) = CreateClientServer();
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            int totalReceived = 0;
            listener.Connected += (sender, args) =>
            {
                args.Session.Send(memory); 
                args.Session.Disconnect();
            };
            connector.Connected += (sender, args) =>
            {
                args.Session.Received += (o, eventArgs) =>
                {
                    totalReceived += eventArgs.Buffer.Length;
                };
                args.Session.Disconnected += (o, eventArgs) =>
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

        [Test]
        public void SessionSendsDataAndPeerReceivesFragmented()
        {
            var (listener, connector) = CreateClientServer();
            var memory = new Memory<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            listener.Connected += (sender, args) =>
            {
                Task.Run(async () =>
                {
                    args.Session.Send(memory.Slice(0, 5));
                    await Task.Delay(50);
                    args.Session.Send(memory.Slice(5, 5));
                });
            };
            int totalReceived = 0;
            connector.Connected += (sender, args) =>
            {
                args.Session.Received += (o, eventArgs) =>
                {
                    totalReceived += eventArgs.Buffer.Length;

                    if (totalReceived == 10)
                        TestComplete.Set();
                };
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete(500);
        }
    }
}
