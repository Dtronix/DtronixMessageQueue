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

        public event EventHandler<TransportSessionEventArgs> Connected;
        public event EventHandler ConnectionError;

        public void Connect(TransportConfig config)
        {

            var socketBufferPool = new BufferMemoryPool(config.SendAndReceiveBufferSize, 2);

            var mainSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            var eventArg = new TcpTransportAsyncEventArgs(socketBufferPool)
            {
                RemoteEndPoint = Utilities.CreateIPEndPoint(config.Address)
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

                    session = new TcpTransportSession(mainSocket, config, socketBufferPool);
                    session.Connected += (sndr, e) => Connected?.Invoke(sndr, e);

                    session.Connect();
                }
            };

            mainSocket.ConnectAsync(eventArg);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(config.ConnectionTimeout, connectionTimeoutCancellation.Token);
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
