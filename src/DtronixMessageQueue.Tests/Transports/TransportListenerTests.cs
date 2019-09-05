using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DtronixMessageQueue.Transports;
using DtronixMessageQueue.Transports.Tcp;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DtronixMessageQueue.Tests.Transports
{
    public class TransportListenerTests : TransportTestBase
    {
        [Test]
        public void ListenerAcceptsNewConnection()
        {


            var listener = new TcpTransportListener(Config);
            var connector = new TcpTransportClientConnector();

            listener.Connected += (sender, args) => TestComplete.Set();

            connector.Connect(Config);

            WaitTestComplete();
        }
    }

    public class TransportTestBase
    {
        private Exception _lastException;
        public ManualResetEventSlim TestComplete { get; set; }
        public TransportConfig Config { get; set; }

        public Exception LastException
        {
            get => _lastException;
            set
            {
                _lastException = value;
                TestComplete.Set();
            }
        }

        public TransportTestBase()
        {
            TestComplete = new ManualResetEventSlim(false);
            Config = new TransportConfig()
            {
                Address = $"127.0.0.1:{FreeTcpPort()}",
            };

        }

        public static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }


        protected void WaitTestComplete(int time = -1)
        {
            TestComplete.Wait(time);

            if (LastException != null)
                throw LastException;

        }


    }
}
