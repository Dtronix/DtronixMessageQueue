using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DtronixMessageQueue.Tests.Transports
{


    public class TransportListenerTests : TransportTestBase
    {

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ListenerStarts(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);

            listener.Started += (sender, args) => TestComplete.Set();

            listener.Start();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ListenerAcceptsNewConnection(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);

            listener.Connected = session => TestComplete.Set();

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ServerDisconnects(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);

            connector.Connected = session =>
            {
                session.Disconnect();
            };
            listener.Connected = session =>
            {
                session.Disconnected += (o, eventArgs) => TestComplete.Set();
                
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ServerStopsAcceptingAtMaxConnections(TransportType type)
        {
            var (listener, connector1) = CreateClientServer(type);
            var connector2 = CreateClient(type);

            ServerConfig.MaxConnections = 1;

            connector1.Connected = session =>
            {
                Console.WriteLine("Connector 1 connected.  Connector 2 connection started...");
                connector2.Connect();
            };

            connector2.Connected = session =>
            {
                Console.WriteLine("Connector 2 connected.  Waiting for disconnect...");
                session.Disconnected += (o, eventArgs) => TestComplete.Set();
            };

            listener.Started += (sender, args) =>
            {
                Console.WriteLine("Listener started.");
                connector1.Connect();
            };

            Console.WriteLine("Starting test...");
            listener.Start();
            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ServerListens(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);

            Assert.False(listener.IsListening);
            listener.Start();
            Assert.True(listener.IsListening);
            listener.Stop();
            Assert.False(listener.IsListening);
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ServerAcceptsConnectionAfterStop(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            var startCount = 0;

            void OnListenerOnStarted(object sender, EventArgs args)
            {
                if (++startCount == 2)
                {
                    listener.Started -= OnListenerOnStarted;
                    connector.Connect();
                    return;
                }
                listener.Stop();

            }

            void OnListenerOnStopped(object sender, EventArgs args)
            {
                listener.Start();

            }

            connector.Connected = session => TestComplete.Set();

            listener.Started += OnListenerOnStarted;
            listener.Stopped += OnListenerOnStopped;
            listener.Start();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ServerFiresStoppedEvent(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            listener.Started += (sender, args) => listener.Stop();
            listener.Stopped += (sender, args) => TestComplete.Set();
            listener.Start();

            WaitTestComplete();
        }

        [TestCase(TransportType.Tcp)]
        [TestCase(TransportType.TcpAppliction)]
        public void ServerFiresStartedEvent(TransportType type)
        {
            var (listener, connector) = CreateClientServer(type);
            listener.Started += (sender, args) => TestComplete.Set();
            listener.Start();

            WaitTestComplete();
        }
    }
}
