using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using DtronixMessageQueue.Layers;
using DtronixMessageQueue.Layers.Application;
using DtronixMessageQueue.Layers.Application.Tls;
using DtronixMessageQueue.Layers.Application.Transparent;
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
        public TlsApplicationConfig TlsServerConfig { get; set; }
        public TlsApplicationConfig TlsClientConfig { get; set; }
        public ApplicationConfig ApplicationClientConfig { get; set; }
        public ApplicationConfig ApplicationServerConfig { get; set; }


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
        private Stopwatch _stopwatch;

        public TransportTestBase()
        {
            TestComplete = new ManualResetEventSlim(false);
            TlsCertificate = X509Certificate.CreateFromCertFile("Cert.pfx");
            _stopwatch = new Stopwatch();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LastException = args.ExceptionObject as Exception;
                Console.WriteLine(LastException?.StackTrace);
            };
        }

        [SetUp]
        public void Init()
        {
            Thread.Sleep(10);

            Logger = new MqLogger
            {
                MinimumLogLevel = LogEventLevel.Trace
            };

            Logger.LogEvent += OnLoggerOnLogEvent;

            int p = FreeTcpPort(); //Interlocked.Increment(ref port);

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

            ApplicationServerConfig = new ApplicationConfig
            {
                Logger = Logger
            };

            ApplicationClientConfig = new ApplicationConfig
            {
                Logger = Logger
            };

            TlsClientConfig = new TlsApplicationConfig
            {
                Certificate = TlsCertificate,
                CertificateValidationCallback = (sender, certificate, chain, errors)
                    => RemoteCertificateValidationCallback(sender, certificate, chain, errors),
                Logger = Logger
            };

            TlsServerConfig = new TlsApplicationConfig
            {
                Certificate = TlsCertificate,
                CertificateValidationCallback = (sender, certificate, chain, errors)
                    => RemoteCertificateValidationCallback(sender, certificate, chain, errors),
                Logger = Logger
            };

            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            _stopwatch.Restart();
        }


        private void OnLoggerOnLogEvent(object sender, LogEventArgs args)
        {
            Console.WriteLine($"[{(_stopwatch.ElapsedMilliseconds/1000d):00.000}][{args.Level}] {args.Message} ({Path.GetFileNameWithoutExtension(args.Filename)}:{args.SourceLineNumber})");
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
            else if (type == Protocol.TcpTransparent)
            {
                var factory = new TcpTransportFactory(ServerConfig);
                listener = new TransparentApplicationListener(factory, ApplicationServerConfig);
            }
            else if (type == Protocol.TcpTls)
            {
                var factory = new TcpTransportFactory(ServerConfig);
                listener = new TlsApplicationListener(factory, TlsServerConfig);
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
            else if (type == Protocol.TcpTransparent)
            {
                var factory = new TcpTransportFactory(ClientConfig);
                connector = new TransparentApplicationClientConnector(factory, ApplicationClientConfig);
            }
            else if (type == Protocol.TcpTls)
            {
                var factory = new TcpTransportFactory(ClientConfig);
                connector = new TlsApplicationClientConnector(factory, TlsClientConfig);
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