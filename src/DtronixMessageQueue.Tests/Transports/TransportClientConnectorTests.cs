using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DtronixMessageQueue.Tests.Transports
{


    public class TransportClientConnectorTests : TransportTestBase
    {

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void ClientConnects(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);

            connector.Connected = session => TestComplete.Set();

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void ClientDisconnects(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);

            connector.Connected = session =>
            {
                session.Disconnected += (o, eventArgs) => TestComplete.Set();
            };
            listener.Connected = session =>
            {
                session.Disconnect();
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void ClientConnectionTimesOut(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            ClientConfig.ConnectionTimeout = 100;

            connector.ConnectionError = () =>
            {
                TestComplete.Set();
            };

            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void ClientConnectorConnectsAfterDisconnect(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            var totalConnections = 0;

            connector.Connected = session =>
            {
                if (++totalConnections == 2)
                {
                    TestComplete.Set();
                    return;
                }

                session.Disconnected += (o, eventArgs) => connector.Connect();

                session.Disconnect();
            };

            listener.Start();
            connector.Connect();
            
            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.SocketTcp)]
        public void ClientConnectorThrowsOnMultipleSimultaneousConnections(TransportType type)
        {
            var connector = CreateClient(type);
            connector.Connect();

            Assert.Throws<InvalidOperationException>(() => connector.Connect());
        }
    }
}
