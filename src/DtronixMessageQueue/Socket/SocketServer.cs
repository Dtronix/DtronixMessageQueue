using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base functionality for handling connection requests.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class SocketServer<TSession, TConfig> : SessionHandler<TSession, TConfig>
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : SocketConfig
    {
        /// <summary>
        /// Limits the number of active connections.
        /// </summary>
        private readonly Semaphore _connectionLimit;

        /// <summary>
        /// True if the server is listening and accepting connections.  False if the server is closed.
        /// </summary>
        public override bool IsRunning => _isStopped == false && (MainSocket?.IsBound ?? false);

        /// <summary>
        /// Set to true of this socket is stopped.
        /// </summary>
        private bool _isStopped = true;

        /// <summary>
        /// Creates a socket server with the specified configurations.
        /// </summary>
        /// <param name="config">Configurations for this socket.</param>
        public SocketServer(TConfig config) : base(config, SocketMode.Server)
        {
            _connectionLimit = new Semaphore(config.MaxConnections, config.MaxConnections);
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {
            var ip = IPAddress.Parse(Config.Ip);
            var localEndPoint = new IPEndPoint(ip, Config.Port);
            if (_isStopped == false)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            // create the socket which listens for incoming connections
            MainSocket = new System.Net.Sockets.Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            MainSocket.Bind(localEndPoint);

            // start the server with a listen backlog.
            MainSocket.Listen(Config.ListenerBacklog);

            // post accepts on the listening socket
            StartAccept(null);
            _isStopped = false;
        }

        /// <summary>
        /// Begins an operation to accept a connection request from the client 
        /// </summary>
        /// <param name="e">The context object to use when issuing the accept operation on the server's listening socket</param>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (e == null)
            {
                e = new SocketAsyncEventArgs();
                e.Completed += (sender, completedE) => AcceptCompleted(completedE);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                e.AcceptSocket = null;
            }

            _connectionLimit.WaitOne();
            try
            {
                if (MainSocket.AcceptAsync(e) == false)
                {
                    AcceptCompleted(e);
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
            {
                return;
            }

            e.AcceptSocket.NoDelay = true;

            var session = CreateSession(e.AcceptSocket);


            //((ISetupSocketSession<TConfig>)session).Setup(, AsyncPool, Config, ThreadPool);

            // Add event to remove this session from the active client list.
            session.Closed += RemoveClientEvent;

            // Add this session to the list of connected sessions.
            ConnectedSessions.TryAdd(session.Id, session);

            // Start the session.
            ((ISetupSocketSession) session).Start();

            // Invoke the events.
            OnConnect(session);

            // Accept the next connection request
            StartAccept(e);
        }

        /// <summary>
        /// Event called to remove the disconnected session from the list of active connections.
        /// </summary>
        /// <param name="sender">Sender of the disconnection event.</param>
        /// <param name="e">Session events.</param>
        private void RemoveClientEvent(object sender, SessionClosedEventArgs<TSession, TConfig> e)
        {
            TSession sessionOut;
            ConnectedSessions.TryRemove(e.Session.Id, out sessionOut);
            e.Session.Closed -= RemoveClientEvent;
        }

        /// <summary>
        /// Terminates this server and notify all connected clients.
        /// </summary>
        public void Stop()
        {
            if (_isStopped)
            {
                return;
            }
            TSession[] sessions = new TSession[ConnectedSessions.Values.Count];
            ConnectedSessions.Values.CopyTo(sessions, 0);

            foreach (var session in sessions)
            {
                session.Close(SocketCloseReason.ServerClosing);
            }

            MainSocket.Close();
            _isStopped = true;
        }
    }
}