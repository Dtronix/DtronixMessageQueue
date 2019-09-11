using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Transports.Tcp
{
    public class TcpTransportClientConnector : ITransportClientConnector
    {
        private readonly TransportConfig _config;
        private BufferMemoryPool _socketBufferPool;




        public TcpTransportClientConnector(TransportConfig config)
        {
            _config = config;

            _socketBufferPool = new BufferMemoryPool(_config.SendAndReceiveBufferSize, 2);

        }

        public Action<ISession> Connected { get; set; }
        public Action ConnectionError { get; set; }

        public ISession Session { get; private set; }

        private bool _connecting;

        public void Connect()
        {
            if(_connecting)
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
                    session = new TcpTransportSession(mainSocket, _config, _socketBufferPool);

                    SessionCreated?.Invoke(session);

                    // Determine if the socket gave an error.
                    if (args.SocketError != SocketError.Success)
                    {
                        session.Disconnect();
                        return;
                    }

                    session.Connected += (sndr, e) =>
                    {
                        _connecting = false;
                        Session = e.Session;
                        Connected?.Invoke(e.Session);
                    };
                    session.Disconnected += (sndr, e) =>
                    {
                        _connecting = false;
                        Session = null;
                    };

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
                ConnectionError?.Invoke();

            }, connectionTimeoutCancellation.Token);
        }

        public Action<ISession> SessionCreated { get; set; }
    }
}
