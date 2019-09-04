using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using DtronixMessageQueue.TcpSocket;

namespace DtronixMessageQueue.Transports.Tcp
{
    public class TcpTransportListener : ITransportListener
    {
        private readonly TcpTransportConfig _config;

        /// <summary>
        /// Set to the max number of connections allowed for the server.
        /// Decremented when a new connection occurs and incremented when 
        /// </summary>
        private int _remainingConnections;

        /// <summary>
        /// Used to prevent more connections connecting to the server than allowed.
        /// </summary>
        private readonly object _connectionLock = new object();
        

        private Socket MainSocket;

        /// <summary>
        /// True if the server is listening and accepting connections.  False if the server is closed.
        /// </summary>
        public bool IsListening => MainSocket?.IsBound ?? false;

        public event EventHandler<TransportSessionEventArgs> Connected;
        public event EventHandler<TransportSessionEventArgs> Disconnected;
        public event EventHandler Stopped;
        public event EventHandler Started;

        public TcpTransportListener(TcpTransportConfig config)
        {
            _config = config;

            // create the socket which listens for incoming connections
            MainSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
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

        public void Start()
        {
            // Reset the remaining connections.
            _remainingConnections = _config.MaxConnections;

            MainSocket.Bind(localEndPoint);

            // start the server with a listen backlog.
            MainSocket.Listen(_config.ListenerBacklog);

            // post accepts on the listening socket
            StartAccept(null);

            // Invoke the started event.
            Started?.Invoke(this, EventArgs.Empty);

            var localEndPoint = Utilities.CreateIPEndPoint(_config.Address);
            if (IsListening)
            {
                throw new InvalidOperationException("Server is already running.");
            }
        }

        public void Stop()
        {
            if (!IsListening)
                return;

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

            // Invoke the stopped event.
            Stopped?.Invoke(this, EventArgs.Empty);
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

            var session = new TcpTransportSession(e.AcceptSocket, _config, memoryPool);

            // If we are at max sessions, close the new connection with a connection refused reason.
            if (maxSessions)
            {
                session.Close();
            }
            else
            {
                // Add event to remove this session from the active client list.
                session.Closed += RemoveClientEvent;

                // Add this session to the list of connected sessions.
                ConnectedSessions.TryAdd(session.Id, session);

                // Start the session.
                ((ISetupSocketSession)session).StartSession();

                session.Connected += (sender, args) => OnConnect(session);
            }

            // Accept the next connection request
            StartAccept(e);
        }
    }
}
