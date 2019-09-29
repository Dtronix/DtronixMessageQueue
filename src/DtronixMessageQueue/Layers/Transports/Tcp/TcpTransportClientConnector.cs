using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Layers.Transports.Tcp
{
    public class TcpTransportClientConnector : ITransportClientConnector
    {
        private readonly TransportConfig _config;
        private BufferMemoryPool _socketBufferPool;


        public Action<ITransportSession> SessionCreated { get; set; }


        public TcpTransportClientConnector(TransportConfig config)
        {
            _config = config;

            _socketBufferPool = new BufferMemoryPool(_config.SendAndReceiveBufferSize, 2);
        }

        public event EventHandler<SessionEventArgs> Connected;
        public event EventHandler ConnectionError;


        public ISession Session { get; private set; }

        private bool _connecting;

        public void Connect()
        {
            if (_connecting)
                throw new InvalidOperationException("Can't connect when client is already connected.");

            _connecting = true;

            var mainSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            var eventArg = new SocketAsyncEventArgs
            {
                RemoteEndPoint = Utilities.CreateIPEndPoint(_config.Address)
            };

            var timedOut = false;
            var connectionTimeoutCancellation = new CancellationTokenSource();
            TcpTransportSession session = null;

            eventArg.Completed += (sender, args) =>
            {
                if (timedOut)
                {
                    return;
                }

                if (args.LastOperation == SocketAsyncOperation.Connect)
                {
                    // Stop the timeout timer.
                    connectionTimeoutCancellation.Cancel();
                    session = new TcpTransportSession(mainSocket, _config, _socketBufferPool, SessionMode.Client);

                    session.Connected += (sndr, e) =>
                    {
                        _connecting = false;
                        Session = e.Session;
                        Connected?.Invoke(this, e);
                    };
                    session.Disconnected += (sndr, e) =>
                    {
                        _connecting = false;
                        Session = null;
                    };


                    SessionCreated?.Invoke(session);

                    // Determine if the socket gave an error.
                    if (args.SocketError != SocketError.Success)
                    {
                        session.Disconnect();
                        return;
                    }


                    session.Connect();
                }
            };

            mainSocket.ConnectAsync(eventArg);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_config.ConnectionTimeout, connectionTimeoutCancellation.Token);
                }
                catch
                {
                    return;
                }

                timedOut = true;
                _connecting = false;
                session?.Disconnect();
                ConnectionError?.Invoke(this, EventArgs.Empty);
            }, connectionTimeoutCancellation.Token);
        }
    }
}