using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.Transports;
using DtronixMessageQueue.Transports.Tcp;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Transports
{
    public class TransportTestBase
    {
        private Exception _lastException;
        public ManualResetEventSlim TestComplete { get; set; }
        public TransportConfig ServerConfig { get; set; }
        public TransportConfig ClientConfig { get; set; }

        public Exception LastException
        {
            get => _lastException;
            set
            {
                _lastException = value;
                TestComplete.Set();
            }
        }

        public static ConcurrentQueue<int> FreePorts = new ConcurrentQueue<int>();

        public TransportTestBase()
        {
            TestComplete = new ManualResetEventSlim(false);
        }

        [SetUp]
        public void Init()
        {
            ServerConfig = new TransportConfig
            {
                Address = $"127.0.0.1:{FreeTcpPort()}",
            };

            ClientConfig = new TransportConfig()
            {
                Address = ServerConfig.Address
            };
        }


        protected int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        protected (ITransportListener, ITransportClientConnector) 
            CreateClientServer(TransportType type = TransportType.Tcp)
        {
            ITransportListener listener = null;
            ITransportClientConnector connector = CreateClient(type);

            if (type == TransportType.Tcp)
            {
                listener = new TcpTransportListener(ServerConfig);
            }

            return (listener, connector);
        }

        protected ITransportClientConnector CreateClient(TransportType type = TransportType.Tcp)
        {
            ITransportClientConnector connector = null;

            if (type == TransportType.Tcp)
            {
                connector = new TcpTransportClientConnector(ClientConfig);
            }

            return connector;
        }


        protected void WaitTestComplete(int time = 1000000)
        {
            if (!TestComplete.Wait(time))
                throw new TimeoutException($"Test timed out at {time}ms");

            if (LastException != null)
                throw LastException;

        }


    }
}