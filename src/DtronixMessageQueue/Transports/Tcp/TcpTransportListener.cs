using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.Transports.Tcp
{
    public class TcpTransportListener : ITransportListener
    {
        private readonly TransportConfig _config;

        /// <summary>
        /// Set to the max number of connections allowed for the server.
        /// Decremented when a new connection occurs and incremented when 
        /// </summary>
        private int _remainingConnections;

        /// <summary>
        /// Used to prevent more connections connecting to the server than allowed.
        /// </summary>
        private readonly object _connectionLock = new object();
        

        private Socket _mainSocket;
        private BufferMemoryPool _socketBufferPool;

        /// <summary>
        /// True if the server is listening and accepting connections.  False if the server is closed.
        /// </summary>
        public bool IsListening => _mainSocket?.IsBound ?? false;

        public event EventHandler<TransportSessionEventArgs> Connected;
        public event EventHandler<TransportSessionEventArgs> Disconnected;
        public event EventHandler Stopped;
        public event EventHandler Started;

        public TcpTransportListener(TransportConfig config)
        {
            _config = config;

            var maxConnections = _config.MaxConnections + 1;

            _socketBufferPool = new BufferMemoryPool(_config.SendAndReceiveBufferSize, 2 * maxConnections);
        }


        /// <summary>
        /// Begins an operation to accept a connection request from the client 
        /// </summary>
        /// <param name="e">The context object to use when issuing the accept operation on the server's listening socket</param>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (!IsListening)
                return;

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
                if (_mainSocket.AcceptAsync(e) == false)
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
            if (IsListening)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            // create the socket which listens for incoming connections
            _mainSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            // Reset the remaining connections.
            _remainingConnections = _config.MaxConnections;
            
            _mainSocket.Bind(Utilities.CreateIPEndPoint(_config.Address));

            // start the server with a listen backlog.
            _mainSocket.Listen(_config.ListenerBacklog);

            // post accepts on the listening socket
            StartAccept(null);

            // Invoke the started event.
            Started?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (!IsListening)
                return;

            try
            {
                _mainSocket.Shutdown(SocketShutdown.Both);
                _mainSocket.Disconnect(true);
            }
            catch
            {
                //ignored
            }
            finally
            {
                _mainSocket.Close();
                _mainSocket = null;
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
            if (!IsListening)
                return;

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

            // If we are at max sessions, close the new connection with a connection refused reason.
            if (maxSessions)
            {
                try
                {
                    e.AcceptSocket.Disconnect(false);
                }
                catch
                {
                    // Ignore
                }
                
            }
            else
            {
                var session = new TcpTransportSession(e.AcceptSocket, _config, _socketBufferPool);

                session.Disconnected += (sender, args) => Disconnected?.Invoke(this, args);
                session.Connected += (sender, args) => Connected?.Invoke(this, args);
                session.Connect();
            }

            // Accept the next connection request
            StartAccept(e);
        }
    }
}
