using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Transports.Tcp;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DtronixMessageQueue.Tests.Transports
{


    public class TransportClientConnectorTests : TransportTestBase
    {

        [Test]
        public void ClientConnects()
        {
            var (listener, connector) = CreateClientServer();

            connector.Connected += (sender, args) => TestComplete.Set();

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [Test]
        public void ClientDisconnects()
        {
            var (listener, connector) = CreateClientServer();

            connector.Connected += (sender, args) =>
                {
                    args.Session.Disconnected += (o, eventArgs) => TestComplete.Set();
                };
            listener.Connected += (sender, args) =>
            {
                args.Session.Disconnect();
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [Test]
        public void ClientConnectionTimesOut()
        {
            var (listener, connector) = CreateClientServer();
            ClientConfig.ConnectionTimeout = 100;

            connector.ConnectionError += (sender, args) =>
            {
                TestComplete.Set();
            };

            connector.Connect();

            WaitTestComplete();
        }

        [Test]
        public void ClientConnectorThrowsOnMultipleSimultaneousConnections()
        {
            var connector = CreateClient();
            connector.Connect();

            Assert.Throws<InvalidOperationException>(() => connector.Connect());
        }
    }
}
