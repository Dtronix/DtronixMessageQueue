using System;
using System.Net;
using System.Net.Sockets;

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
        /// Set to the max number of connections allowed for the server.
        /// Decremented when a new connection occurs and incremented when 
        /// </summary>
        private int _remainingConnections;

        private readonly object _connectionLock = new object();

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
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {
            // Reset the remaining connections.
            _remainingConnections = Config.MaxConnections;

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
            MainSocket.Listen(1);

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

            var session = CreateSession(e.AcceptSocket);

            if (maxSessions)
            {

                session.Close(SocketCloseReason.ConnectionRefused);
            }
            else
            {
                // Add event to remove this session from the active client list.
                session.Closed += RemoveClientEvent;

                // Add this session to the list of connected sessions.
                ConnectedSessions.TryAdd(session.Id, session);

                // Start the session.
                ((ISetupSocketSession)session).Start();

                // Invoke the events.
                OnConnect(session);
            }

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
            // Remove the session from the list of active sessions and release the semaphore.
            if (ConnectedSessions.TryRemove(e.Session.Id, out sessionOut))
            {
                // If the remaining connection is now 1, that means that the server need to begin
                // accepting new client connections.
                lock (_connectionLock)
                    _remainingConnections++;

            }

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