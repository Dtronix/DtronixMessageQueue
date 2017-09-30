using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer.Tcp
{
    public class TcpTransportLayer : ITransportLayer
    {

        public TransportLayerMode Mode { get; }
        public TransportLayerState State { get; private set; }

        public event EventHandler<TransportLayerEventArgs> Started;

        public event EventHandler<TransportLayerStopEventArgs> Stopping;
        public event EventHandler<TransportLayerStopEventArgs> Stopped;

        public event EventHandler<TransportLayerSessionEventArgs> Connected;

        public event EventHandler<TransportLayerSessionCloseEventArgs> Closing;
        public event EventHandler<TransportLayerSessionCloseEventArgs> Closed;

        public TransportLayerConfig Config { get; }

        public bool IsListening { get; private set; }

        public ConcurrentDictionary<Guid, ITransportLayerSession> ConnectedSessions { get; private set; }

        /// <summary>
        /// Used to prevent more connections connecting to the server than allowed.
        /// </summary>
        private readonly object _connectionLock = new object();
        /// <summary>
        /// Set to the max number of connections allowed for the server.
        /// Decremented when a new connection occurs and incremented when 
        /// </summary>
        private int _remainingConnections;

        /// <summary>
        /// Main socket used by the child class for connection or for the listening of incoming connections.
        /// </summary>
        protected System.Net.Sockets.Socket MainSocket;

        private SocketAsyncEventArgs listenEventArgs;

        /// <summary>
        /// Pool of async args for sessions to use.
        /// </summary>
        public SocketAsyncEventArgsManager AsyncManager { get; }

        public TcpTransportLayer(TransportLayerConfig config, TransportLayerMode mode)
        {
            Config = config;
            Mode = mode;

            // Use the max connections plus one for the disconnecting of 
            // new clients when the MaxConnections has been reached.
            var maxConnections = Config.MaxConnections + 1;

            // preallocate pool of SocketAsyncEventArgs objects
            AsyncManager = new SocketAsyncEventArgsManager(Config.SendAndReceiveBufferSize * maxConnections * 2,
                Config.SendAndReceiveBufferSize);
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {
            if(Mode != TransportLayerMode.Server)
                throw new InvalidOperationException("Transport layer is running in client mode.  Start is a server mode method.");

            // Reset the remaining connections.
            _remainingConnections = Config.MaxConnections;

            var localEndPoint = Utilities.CreateIPEndPoint(Config.ConnectAddress);

            if (IsListening)
                throw new InvalidOperationException("Server is already listening for connections");

            // create the socket which listens for incoming connections
            MainSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            MainSocket.Bind(localEndPoint);

            // start the server with a listen backlog.
            MainSocket.Listen(Config.ListenerBacklog);

            // Invoke the started event.
            Started?.Invoke(this, new TransportLayerEventArgs(this));
        }

        /// <summary>
        /// Begins an operation to accept a connection request from the client 
        /// </summary>
        public void AcceptSession()
        {
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

        public void Connect()
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
                    OnConnect(Session);

                    ConnectedSessions.TryAdd(Session.Id, Session);

                    ((ISocketSession)Session).Start();
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

        public void Close(SessionCloseReason reason)
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

        /// <summary>
        /// Called by the socket when a new connection has been accepted.
        /// </summary>
        /// <param name="e">Event args for this event.</param>
        private void AcceptCompleted(SocketAsyncEventArgs e)
        {
            if (MainSocket.IsBound == false)
            {
                return;
            }

            bool maxSessions = false;

            // Check if we are maxed out on concurrent connections.
            // If so, stop listening for new connections until we can accept a new connection
            lock (_connectionLock)
            {

                if (_remainingConnections == 0)
                {
                    maxSessions = true;
                }
                else
                {
                    _remainingConnections--;
                }
            }

            e.AcceptSocket.NoDelay = true;

            var session = new TcpTransportLayerSession(this, e.AcceptSocket);

            // Add the connection and closing events.
            session.Closing += SessionOnClosing;
            session.Closed += SessionOnClosed;

            // If we are at max sessions, close the new connection with a connection refused reason.
            if (maxSessions)
            {
                session.Close(SessionCloseReason.ConnectionRefused);
                return;
            }

            // Add this session to the list of connected sessions.
            ConnectedSessions.TryAdd(session.Id, session);

            // Start to receive data on this session.
            session.Receive();

            // Fire off the connected events.
            Connected?.Invoke(this, new TransportLayerSessionEventArgs(session));

        }



        private void SessionOnClosed(object sender, TransportLayerSessionCloseEventArgs e)
        {
            // Remove all the events.
            e.Session.Closing -= SessionOnClosing;
            e.Session.Closed -= SessionOnClosed;
        }

        private void SessionOnClosing(object sender, TransportLayerSessionCloseEventArgs e)
        {
            Closing?.Invoke(sender, e);
        }

        /// <summary>
        /// Terminates this server and notify all connected clients.
        /// </summary>
        public void Stop()
        {
            if (Mode != TransportLayerMode.Server)
                throw new InvalidOperationException("Transport layer is running in client mode.  Start is a server mode method.");

            if (State != TransportLayerState.Connected)
                return;

            State = TransportLayerState.Closing;
            var closeReason = SessionCloseReason.ServerClosing;

            Stopping?.Invoke(this, new TransportLayerStopEventArgs(this, closeReason));

            ITransportLayerSession[] sessions = new ITransportLayerSession[ConnectedSessions.Values.Count];
            ConnectedSessions.Values.CopyTo(sessions, 0);

            foreach (var session in sessions)
            {
                session.Close(closeReason);
            }

            try
            {
                MainSocket.Shutdown(SocketShutdown.Both);
                MainSocket.Disconnect(true);
            }
            catch
            {
                //ignored
            }
            finally
            {
                MainSocket.Close();
            }

            State = TransportLayerState.Closed;

            // Invoke the stopped event.
            Stopped?.Invoke(this, new TransportLayerStopEventArgs(this, SessionCloseReason.ServerClosing));
        }

    }
}
