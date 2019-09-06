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

        public void Connect()
        {

            var mainSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            var eventArg = new SocketAsyncEventArgs()
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
                    session.Connected += (sndr, e) => Connected?.Invoke(sndr, e);

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
                session.Disconnect();
                ConnectionError?.Invoke(this, EventArgs.Empty);

            }, connectionTimeoutCancellation.Token);
        }
    }
}
