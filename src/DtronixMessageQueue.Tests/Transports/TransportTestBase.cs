using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.ApplicationLayers;
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
        protected MqLogger Logger;
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

        public static int port = 25000;

        [SetUp]
        public void Init()
        {
            //Thread.Sleep(10);

            Logger = new MqLogger
            {
                MinimumLogLevel = LogEventLevel.Trace
            };

            Logger.LogEvent += OnLoggerOnLogEvent;

            int p = FreeTcpPort();//Interlocked.Increment(ref port);

            ServerConfig = new TransportConfig
            {
                Address = $"127.0.0.1:{p}",
                Logger = Logger
            };

            ClientConfig = new TransportConfig
            {
                Address = ServerConfig.Address,
                Logger = Logger
            };
        }

        private void OnLoggerOnLogEvent(object sender, LogEventArgs args)
        {
            Console.WriteLine($"[{args.Level}] {args.Message} ({Path.GetFileNameWithoutExtension(args.Filename)}:{args.SourceLineNumber})");
        }

        [TearDown]
        public void Cleanup()
        {
            Logger.LogEvent -= OnLoggerOnLogEvent;
            LastException = null;
            TestComplete.Reset();
        }


        protected int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        protected (IListener, IClientConnector) 
            CreateClientServer(TransportType type = TransportType.Tcp)
        {
            IListener listener = null;
            IClientConnector connector = CreateClient(type);


            if (type == TransportType.Tcp)
            {
                listener = new TcpTransportListener(ServerConfig);
            }
            else if (type == TransportType.SocketTcp)
            {
                var factory = new TcpTransportFactory(ServerConfig);
                listener = new SocketListener(factory);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return (listener, connector);
        }



        protected IClientConnector CreateClient(TransportType type = TransportType.Tcp)
        {
            IClientConnector connector = null;


            if (type == TransportType.Tcp)
            {
                connector = new TcpTransportClientConnector(ClientConfig);
            }
            else if (type == TransportType.SocketTcp)
            {
                var factory = new TcpTransportFactory(ClientConfig);
                connector = new SocketClientConnector(factory);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return connector;
        }


        protected void WaitTestComplete(int time = 2000)
        {
            if (!TestComplete.Wait(time))
                throw new TimeoutException($"Test timed out at {time}ms");

            if (LastException != null)
                throw LastException;

        }


    }
}