using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using DtronixMessageQueue.Layers;
using DtronixMessageQueue.Layers.Application;
using DtronixMessageQueue.Layers.Application.Tls;
using DtronixMessageQueue.Layers.Transports;
using DtronixMessageQueue.Layers.Transports.Tcp;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Transports
{
    public class TransportTestBase
    {
        private Exception _lastException;
        public ManualResetEventSlim TestComplete { get; set; }
        public TransportConfig ServerConfig { get; set; }
        public TransportConfig ClientConfig { get; set; }
        public TlsLayerConfig TlsServerConfig { get; set; }
        public TlsLayerConfig TlsClientConfig { get; set; }

        public X509Certificate TlsCertificate { get; set; }
        protected MqLogger Logger;

        private ConcurrentBag<IClientConnector> _clientConnectors = new ConcurrentBag<IClientConnector>();
        private ConcurrentBag<IListener> _listeners = new ConcurrentBag<IListener>();

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

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
            TlsCertificate = X509Certificate.CreateFromCertFile("Cert.pfx");
            
        }

        public static int port = 25000;

        [SetUp]
        public void Init()
        {
            Thread.Sleep(10);

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

            TlsClientConfig = new TlsLayerConfig
            {
                Certificate = TlsCertificate,
                CertificateValidationCallback = (sender, certificate, chain, errors) 
                    => RemoteCertificateValidationCallback(sender, certificate, chain, errors)
            };

            TlsServerConfig = new TlsLayerConfig
            {
                Certificate = TlsCertificate,
                CertificateValidationCallback = (sender, certificate, chain, errors)
                    => RemoteCertificateValidationCallback(sender, certificate, chain, errors)
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
            RemoteCertificateValidationCallback = null;
            TestComplete.Reset();

            var listeners = _listeners.ToArray();
            var clientConnectors = _clientConnectors.ToArray();

            foreach (var listener in listeners)
            {
                listener.Stop();
            }
            foreach (var connector in clientConnectors)
            {
                connector.Session?.Disconnect();
            }

            _clientConnectors.Clear();
            _listeners.Clear();
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
            CreateClientServer(Protocol type = Protocol.Tcp)
        {
            IListener listener = null;
            IClientConnector connector = CreateClient(type);


            if (type == Protocol.Tcp)
            {
                listener = new TcpTransportListener(ServerConfig);
            }
            else if (type == Protocol.TcpAppliction)
            {
                var factory = new TcpTransportFactory(ServerConfig);
                listener = new ApplicationListener(factory);
            }
            else if (type == Protocol.TcpTls)
            {
                var factory = new TcpTransportFactory(ServerConfig);
                listener = new TlsLayerListener(factory, TlsServerConfig);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            _listeners.Add(listener);

            return (listener, connector);
        }



        protected IClientConnector CreateClient(Protocol type = Protocol.Tcp)
        {
            IClientConnector connector = null;


            if (type == Protocol.Tcp)
            {
                connector = new TcpTransportClientConnector(ClientConfig);
            }
            else if (type == Protocol.TcpAppliction)
            {
                var factory = new TcpTransportFactory(ClientConfig);
                connector = new ApplicationClientConnector(factory);
            }
            else if (type == Protocol.TcpTls)
            {
                var factory = new TcpTransportFactory(ClientConfig);
                connector = new TlsLayerClientConnector(factory, TlsClientConfig);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            _clientConnectors.Add(connector);

            return connector;
        }


        protected void WaitTestComplete(int time = 200000)
        {
            if (!TestComplete.Wait(time))
                throw new TimeoutException($"Test timed out at {time}ms");

            if (LastException != null)
                throw LastException;

        }


    }
}