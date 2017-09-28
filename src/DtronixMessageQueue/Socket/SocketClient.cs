using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base functionality for all client connections to a remote server.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class SocketClient<TSession, TConfig> : SessionHandler<TSession, TConfig>
        where TSession : DtronixMessageQueue.MqSession<TSession, TConfig>, new()
        where TConfig : TransportLayerConfig
    {
        /// <summary>
        /// True if the client is connected to a server.
        /// </summary>
        public override bool IsRunning => MainSocket?.Connected ?? false;


        /// <summary>
        /// Session for this client.
        /// </summary>
        public TSession Session { get; private set; }

        /// <summary>
        /// Creates a socket client with the specified configurations.
        /// </summary>
        /// <param name="config">Configurations to use.</param>
        public SocketClient(TConfig config) : base(config, TransportLayerMode.Client)
        {
            // Override the number of processors to one for each sending queue and receiving queue.
            config.ProcessorThreads = 1;
        }

        /// <summary>
        /// Connects to the configured endpoint.
        /// </summary>
        public void Connect()
        {
            Connect(new IPEndPoint(IPAddress.Parse(Config.Ip), Config.Port));
        }

        /// <summary>
        /// Cancellation token to cancel the timeout event for connections.
        /// </summary>
        private CancellationTokenSource _connectionTimeoutCancellation;

        /// <summary>
        /// Connects to the specified endpoint.
        /// </summary>
        /// <param name="endPoint">Endpoint to connect to.</param>
        public void Connect(IPEndPoint endPoint)
        {
            if (MainSocket != null && Session?.CurrentState != DtronixMessageQueue.MqSession<TSession, TConfig>.State.Closed)
            {
                throw new InvalidOperationException("Client is in the process of connecting.");
            }

            MainSocket = new System.Net.Sockets.Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            // Set to true if the client connection either timed out or was canceled.
            bool timedOut = false;

            _connectionTimeoutCancellation?.Cancel();

            _connectionTimeoutCancellation = new CancellationTokenSource();




            var eventArg = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint
            };

            eventArg.Completed += (sender, args) =>
            {
                if (timedOut)
                {
                    return;
                }
                if (args.LastOperation == SocketAsyncOperation.Connect)
                {
                    // Stop the timeout timer.
                    _connectionTimeoutCancellation.Cancel();

                    Session = CreateSession(MainSocket);
                    Session.Connected += (sndr, e) => OnConnect(Session);

                    ConnectedSessions.TryAdd(Session.Id, Session);

                    ((ISocketSession) Session).Start();
                }
            };

            MainSocket.ConnectAsync(eventArg);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Config.ConnectionTimeout, _connectionTimeoutCancellation.Token);
                }
                catch
                {
                    return;
                }

                timedOut = true;
                OnClose(null, SessionCloseReason.TimeOut);
                MainSocket.Close();
            }, _connectionTimeoutCancellation.Token);
        }

        protected override void OnClose(TSession session, SessionCloseReason reason)
        {
            MainSocket.Close();

            TSession sessOut;

            // If the session is null, the connection timed out while trying to connect.
            if (session != null)
            {
                ConnectedSessions.TryRemove(Session.Id, out sessOut);
            }

            base.OnClose(session, reason);
        }
    }
}