using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Transports;
using DtronixMessageQueue.Transports.Tcp;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DtronixMessageQueue.Tests.Transports
{


    public class TransportListenerTests : TransportTestBase
    {

        [Test]
        public void ListenerStarts()
        {
            var (listener, connector) = CreateClientServer();

            listener.Started += (sender, args) => TestComplete.Set();

            listener.Start();

            WaitTestComplete();
        }

        [Test]
        public void ListenerAcceptsNewConnection()
        {
            var (listener, connector) = CreateClientServer();

            listener.Connected += (sender, args) => TestComplete.Set();

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [Test]
        public void ServerDisconnects()
        {
            var (listener, connector) = CreateClientServer();

            connector.Connected += (sender, args) =>
            {
                args.Session.Disconnect();
            };
            listener.Connected += (sender, args) =>
            {
                args.Session.Disconnected += (o, eventArgs) => TestComplete.Set();
                
            };

            listener.Start();
            connector.Connect();

            WaitTestComplete();
        }

        [Test]
        public void ServerStopsAcceptingAtMaxConnections()
        {
            var (listener, connector1) = CreateClientServer();
            var connector2 = CreateClient();

            ServerConfig.MaxConnections = 1;

            connector1.Connected += (sender, args) =>
            {
                connector2.Connect();
            };

            connector2.Connected += (sender, args) =>
            {
                args.Session.Disconnected += (o, eventArgs) => TestComplete.Set();
            };

            listener.Started += (sender, args) => connector1.Connect();

            listener.Start();
            WaitTestComplete();
        }



        [Test]
        public void ServerListens()
        {
            var (listener, connector) = CreateClientServer();

            Assert.False(listener.IsListening);
            listener.Start();
            Assert.True(listener.IsListening);
            listener.Stop();
            Assert.False(listener.IsListening);
        }

        [Test]
        public void ServerFiresStoppedEvent()
        {
            var (listener, connector) = CreateClientServer();
            listener.Started += (sender, args) => listener.Stop();
            listener.Stopped += (sender, args) => TestComplete.Set();
            listener.Start();

            WaitTestComplete();
        }

        [Test]
        public void ServerFiresStartedEvent()
        {
            var (listener, connector) = CreateClientServer();
            listener.Started += (sender, args) => TestComplete.Set();
            listener.Start();

            WaitTestComplete();
        }
    }
}
