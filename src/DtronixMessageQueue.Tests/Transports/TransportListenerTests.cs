using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DtronixMessageQueue.Tests.Transports
{


    public class TransportListenerTests : TransportTestBase
    {

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ListenerStarts(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);

            listener.Started += (sender, args) => TestComplete.Set();

            listener.Start();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ListenerAcceptsNewConnection(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);

            listener.Connected += (o, e) => TestComplete.Set();

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerDisconnects(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);

            connector.Connected += (o, e) =>
            {
                e.Session.Disconnect();
            };
            listener.Connected += (o, e) =>
            {
                e.Session.Disconnected += (o, eventArgs) => TestComplete.Set();
                
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerStopsAcceptingAtMaxConnections(Protocol type)
        {
            var (listener, connector1) = CreateClientServer(type);
            var connector2 = CreateClient(type);

            ServerConfig.MaxConnections = 1;

            connector1.Connected += (o, e) =>
            {
                Console.WriteLine("Connector 1 connected.  Connector 2 connection started...");
                connector2.Connect();
            };

            connector2.Connected += (o, e) =>
            {
                Console.WriteLine("Connector 2 connected.  Waiting for disconnect...");
                e.Session.Disconnected += (o, eventArgs) => TestComplete.Set();
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

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerListens(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);

            Assert.False(listener.IsListening);
            listener.Start();
            Assert.True(listener.IsListening);
            listener.Stop();
            Assert.False(listener.IsListening);
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerAcceptsConnectionAfterStop(Protocol type)
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

            connector.Connected += (o, e) => TestComplete.Set();

            listener.Started += OnListenerOnStarted;
            listener.Stopped += OnListenerOnStopped;
            listener.Start();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerFiresStoppedEvent(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            listener.Started += (sender, args) => listener.Stop();
            listener.Stopped += (sender, args) => TestComplete.Set();
            listener.Start();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerFiresStartedEvent(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            listener.Started += (sender, args) => TestComplete.Set();
            listener.Start();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerAcceptsMultipleConnections(Protocol type)
        {
            var (listener, connector) = CreateClientServer(type);
            var connector2 = CreateClient(type);
            var totalConnections = 0;

            listener.Started += (sender, args) =>
            {
                connector.Connect();
                connector2.Connect();
            };

            listener.Connected += (sender, args) =>
            {
                var currentTotal = Interlocked.Increment(ref totalConnections);
                if(currentTotal == 2)
                    TestComplete.Set();
            };
            listener.Start();

            WaitTestComplete();
        }

        [TestCase(Protocol.Tcp)]
        [TestCase(Protocol.TcpTransparent)]
        [TestCase(Protocol.TcpTls)]
        public void ServerLimitsMultipleConnections(Protocol type)
        {
            ServerConfig.MaxConnections = 1;

            var (listener, connector) = CreateClientServer(type);
            var connector2 = CreateClient(type);

            listener.Started += (sender, args) =>
            {
                connector.Connect();
            };

            connector.Connected += (sender, args) => { connector2.Connect(); };
            connector2.Connected += (sender, args) =>
            {
                args.Session.Disconnected += (o, eventArgs) => TestComplete.Set();
            };

            listener.Start();

            WaitTestComplete();
        }
    }
}
