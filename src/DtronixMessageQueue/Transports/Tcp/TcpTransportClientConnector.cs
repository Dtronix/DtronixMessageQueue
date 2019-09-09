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

        public event EventHandler<TransportSessionEventArgs> Connected;
        public event EventHandler ConnectionError;



        public TcpTransportClientConnector(TransportConfig config)
        {
            _config = config;

            _socketBufferPool = new BufferMemoryPool(_config.SendAndReceiveBufferSize, 2);

        }

        public bool IsConnected { get; private set; }

        public void Connect()
        {
            if(IsConnected)
                throw new InvalidOperationException("Can't connect when client is already connected.");

            IsConnected = true;

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

                    // Determine if the socket gave an error.
                    if (args.SocketError != SocketError.Success)
                    {
                        session.Disconnect();

                        return;
                    }

                    session.Connected += (sndr, e) => Connected?.Invoke(sndr, e);
                    session.Disconnected += (o, eventArgs) => IsConnected = false;

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
                session?.Disconnect();
                ConnectionError?.Invoke(this, EventArgs.Empty);

            }, connectionTimeoutCancellation.Token);
        }
    }
}
