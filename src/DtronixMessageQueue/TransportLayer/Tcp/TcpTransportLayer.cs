using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer.Tcp
{
    public class TcpTransportLayer : ITransportLayer
    {

        public TransportLayerMode Mode { get; }
        public TransportLayerState State { get; private set; }

        public event EventHandler<TransportLayerStateChangedEventArgs> StateChanged;
        public event EventHandler<TransportLayerReceiveAsyncEventArgs> Received;

        public TransportLayerConfig Config { get; }

        public bool IsListening { get; private set; }

        public ConcurrentDictionary<Guid, ITransportLayerSession> ConnectedSessions { get; private set; }

        public ITransportLayerSession ClientSession { get; private set; }
        /// <summary>
        /// Main socket used by the child class for connection or for the listening of incoming connections.
        /// </summary>
        protected System.Net.Sockets.Socket MainSocket;

        private SocketAsyncEventArgs listenEventArgs;

        private CancellationTokenSource _connectionTimeoutCancellation;

        /// <summary>
        /// Pool of async args for sessions to use.
        /// </summary>
        public SocketAsyncEventArgsManager AsyncManager { get; }

        public TcpTransportLayer(TransportLayerConfig config, TransportLayerMode mode)
        {
            Config = config;
            Mode = mode;

            ConnectedSessions = new ConcurrentDictionary<Guid, ITransportLayerSession>();

            // Use the max connections plus one for the disconnecting of 
            // new clients when the MaxConnections has been reached.
            var maxConnections = Config.MaxConnections + 1;

            // preallocate pool of SocketAsyncEventArgs objects
            AsyncManager = new SocketAsyncEventArgsManager(Config.SendAndReceiveBufferSize * maxConnections * 2,
                Config.SendAndReceiveBufferSize);

            if(mode == TransportLayerMode.Server)
                State = TransportLayerState.Stopped;
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {
            if(Mode != TransportLayerMode.Server)
                throw new InvalidOperationException("Transport layer is running in client mode.");

            if (State != TransportLayerState.Stopped)
                throw new InvalidOperationException("Server can not be started again while running.");

            State = TransportLayerState.Starting;
            StateChanged?.Invoke(this, new TransportLayerStateChangedEventArgs(this, TransportLayerState.Starting));

            var localEndPoint = Utilities.CreateIPEndPoint(Config.BindAddress);

            if (IsListening)
                throw new InvalidOperationException("Server is already listening for connections");

            // create the socket which listens for incoming connections
            MainSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            MainSocket.Bind(localEndPoint);

            // start the server with a listen backlog.
            MainSocket.Listen(Config.ListenerBacklog);

            MainSocket.NoDelay = true;

            State = TransportLayerState.Started;
            StateChanged?.Invoke(this, new TransportLayerStateChangedEventArgs(this, TransportLayerState.Started));

            // Accept the first connection.
            AcceptAsync();
        }


        /// <summary>
        /// Terminates this server and notify all connected clients.
        /// </summary>
        public void Stop()
        {
            if (Mode != TransportLayerMode.Server)
                throw new InvalidOperationException("Transport layer is running in client mode.  Start is a server mode method.");

            if (State != TransportLayerState.Started)
                return;

            State = TransportLayerState.Stopping;
            var closeReason = SessionCloseReason.Closing;

            StateChanged?.Invoke(this, new TransportLayerStateChangedEventArgs(this, TransportLayerState.Stopping)
            {
                Reason = closeReason
            });

            ITransportLayerSession[] sessions = new ITransportLayerSession[ConnectedSessions.Values.Count];
            ConnectedSessions.Values.CopyTo(sessions, 0);

            foreach (var session in sessions)
            {
                session.Close(closeReason);
            }

            try
            {
                if (MainSocket.Connected)
                {
                    MainSocket.Shutdown(SocketShutdown.Both);
                    MainSocket.Disconnect(true);
                }
            }
            catch
            {
                //ignored
            }
            finally
            {
                MainSocket.Close();
            }

            State = TransportLayerState.Stopped;

            // Invoke the stopped event.
            StateChanged?.Invoke(this, new TransportLayerStateChangedEventArgs(this, TransportLayerState.Stopped)
            {
                Reason = closeReason
            });
        }

        /// <summary>
        /// Begins an operation to accept a connection request from the client 
        /// </summary>
        public void AcceptAsync()
        {
            if (Mode != TransportLayerMode.Server)
                throw new InvalidOperationException("Transport layer is running in client mode.  Start is a server mode method.");

            if (listenEventArgs == null)
            {
                IsListening = true;
                listenEventArgs = new SocketAsyncEventArgs();
                listenEventArgs.Completed += (sender, completedE) => AcceptCompleted(completedE);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                listenEventArgs.AcceptSocket = null;
            }


            try
            {
                if (MainSocket.AcceptAsync(listenEventArgs) == false)
                {
                    AcceptCompleted(listenEventArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                // ignored
            }
        }

        /// <summary>
        /// Called by the socket when a new connection has been accepted.
        /// </summary>
        /// <param name="e">Event args for this event.</param>
        private void AcceptCompleted(SocketAsyncEventArgs e)
        {
            if (MainSocket.IsBound == false)
                return;

            var session = CreateSession(e.AcceptSocket);

            // Fire off the connected events.
            StateChanged?.Invoke(this,
                new TransportLayerStateChangedEventArgs(this, TransportLayerState.Connected, session));
        }


        public void Connect()
        {
            if (Mode != TransportLayerMode.Client)
                throw new InvalidOperationException("Transport layer is running in server mode.");


            if (MainSocket != null && State != TransportLayerState.Closed)
                throw new InvalidOperationException("Client is in the process of connecting.");

            var endPoint = Utilities.CreateIPEndPoint(Config.ConnectAddress);

            MainSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
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
                    ClientSession = CreateSession(MainSocket);

                    State = TransportLayerState.Connected;
                    // Fire off the connected events.
                    StateChanged?.Invoke(this,
                        new TransportLayerStateChangedEventArgs(this, TransportLayerState.Connected, ClientSession));
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
                Close(SessionCloseReason.TimeOut);

                MainSocket.Close();

            }, _connectionTimeoutCancellation.Token);
        }

        public void Close(SessionCloseReason reason)
        {
            if (Mode != TransportLayerMode.Client)
                throw new InvalidOperationException("Transport layer is running in server mode.");

            if (State != TransportLayerState.Connected)
                return;

            State = TransportLayerState.Closing;

            MainSocket.Close();

            ClientSession?.Close(reason);

            State = TransportLayerState.Closed;
        }


        private ITransportLayerSession CreateSession(Socket socket)
        {
            socket.NoDelay = true;

            var session = new TcpTransportLayerSession(this, socket);

            // Add the connection and closing events.
            session.StateChanged += SessionStateChanged;
            session.Received += SessionReceived;

            // Add this session to the list of connected sessions.
            ConnectedSessions.TryAdd(session.Id, session);

            // Start to receive data on this session.
            session.ReceiveAsync();

            return session;
        }

        private void SessionReceived(object sender, TransportLayerReceiveAsyncEventArgs e)
        {
            Received?.Invoke(this, e);
        }

        private void SessionStateChanged(object sender, TransportLayerStateChangedEventArgs e)
        {
            switch (e.State)
            {
                case TransportLayerState.Closed:
                    e.Session.StateChanged -= SessionStateChanged;
                    e.Session.Received -= SessionReceived;

                    ITransportLayerSession sessOut;
                    ConnectedSessions?.TryRemove(e.Session.Id, out sessOut);
                    break;
            }


            StateChanged?.Invoke(this, e);
        }
    }
}
