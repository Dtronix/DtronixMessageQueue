﻿using System;
using System.Net.Sockets;

namespace DtronixMessageQueue.Layers.Transports.Tcp
{
    public class TcpTransportListener : ITransportListener
    {
        protected readonly TransportConfig Config;

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
        private readonly BufferMemoryPool _socketBufferPool;

        /// <summary>
        /// True if the server is listening and accepting connections.  False if the server is closed.
        /// </summary>
        public bool IsListening => _mainSocket?.IsBound ?? false;

        public Action<ITransportSession> SessionCreated { get; set; }

        public event EventHandler<SessionEventArgs> Connected;

        public event EventHandler Stopped;
        public event EventHandler Started;

        public TcpTransportListener(TransportConfig config)
        {
            Config = config;

            _socketBufferPool = new BufferMemoryPool(Config.SendAndReceiveBufferSize, 2 * (Config.MaxConnections + 1));
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
            _remainingConnections = Config.MaxConnections;
            
            _mainSocket.Bind(Utilities.CreateIPEndPoint(Config.Address));

            // start the server with a listen backlog.
            _mainSocket.Listen(Config.ListenerBacklog);

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
                var session = new TcpTransportSession(e.AcceptSocket, Config, _socketBufferPool, SessionMode.Server);
                SessionCreated?.Invoke(session);
                session.Connected += OnConnected;
                session.Connect();
            }

            // Accept the next connection request
            StartAccept(e);
        }

        private void OnConnected(object sender, SessionEventArgs e)
        {
            Connected?.Invoke(this, e);
        }
    }
}