using System;
using System.Net;
using System.Net.Sockets;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.TransportLayer.TcpAsync
{
    public class TcpAsyncTransportLayer : ITransportLayer
    {
        private readonly SocketConfig _config;

        public TransportLayerMode Mode { get; }

        public TransportLayerState State { get; protected set; }

        public bool IsRunning { get; }

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
        /// Async args used to send data to the wire.
        /// </summary>
        private SocketAsyncEventArgs _sendArgs;

        /// <summary>
        /// Async args used to receive data off the wire.
        /// </summary>
        private SocketAsyncEventArgs _receiveArgs;

        /// <summary>
        /// Pool used by all the sessions on this SessionHandler.
        /// </summary>
        private SocketAsyncEventArgsManager _argsPool;


        /// <summary>
        /// Main socket used by the child class for connection or for the listening of incoming connections.
        /// </summary>
        protected System.Net.Sockets.Socket MainSocket;

        /// <summary>
        /// Pool of async args for sessions to use.
        /// </summary>
        protected SocketAsyncEventArgsManager AsyncManager;

        public TcpAsyncTransportLayer(SocketConfig config, TransportLayerMode mode)
        {
            _config = config;

            Mode = mode;

            // Use the max connections plus one for the disconnecting of 
            // new clients when the MaxConnections has been reached.
            var maxConnections = _config.MaxConnections + 1;

            // preallocate pool of SocketAsyncEventArgs objects
            AsyncManager = new SocketAsyncEventArgsManager(_config.SendAndReceiveBufferSize * maxConnections * 2,
                _config.SendAndReceiveBufferSize);
        }


        /// <summary>
        /// This method is called whenever a receive or send operation is completed on a socket 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        protected virtual void IoCompleted(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Disconnect:
                    Close(SocketCloseReason.ClientClosing);
                    break;

                case SocketAsyncOperation.Receive:
                    RecieveComplete(e);
                    break;

                case SocketAsyncOperation.Send:
                    SendComplete(e);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive, send connect or disconnect.");
            }
        }



        /// <summary>
        /// Sends raw bytes to the socket.  Blocks until data is sent.
        /// </summary>
        /// <param name="buffer">Buffer bytes to send.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="length">Total bytes to send.</param>
        protected virtual void Send(byte[] buffer, int offset, int length)
        {
            if (Socket == null || Socket.Connected == false)
                return;

            _writeSemaphore.Wait(-1);

            // Copy the bytes to the block buffer
            Buffer.BlockCopy(buffer, offset, _sendArgs.Buffer, _sendArgs.Offset, length);

            // Update the buffer length.
            _sendArgs.SetBuffer(_sendArgs.Offset, length);

            try
            {
                if (Socket.SendAsync(_sendArgs) == false)
                {
                    IoCompleted(this, _sendArgs);
                }
            }
            catch (ObjectDisposedException)
            {
                Close(SocketCloseReason.SocketError);
            }
        }

        public void Listen()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void AcceptConnection()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// This method is invoked when an asynchronous send operation completes.  
        /// The method issues another receive on the socket to read any additional data sent from the client
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        private void SendComplete(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Close(SocketCloseReason.SocketError);
            }
            _writeSemaphore.Release(1);
        }

        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.
        /// </summary>
        /// <param name="e">Event args of this action.</param>
        protected void RecieveComplete(SocketAsyncEventArgs e)
        {
            if (State == TransportLayerState.Closing)
                return;

            if (e.BytesTransferred == 0 && State == TransportLayerState.Connected)
            {
                State = TransportLayerState.Closing;
                return;
            }
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // Update the last time this session was active to prevent timeout.
                _lastReceived = DateTime.UtcNow;

                // Create a copy of these bytes.
                var buffer = new byte[e.BytesTransferred];

                Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

                HandleIncomingBytes(buffer);

                try
                {
                    // Re-setup the receive async call.
                    if (Socket.ReceiveAsync(e) == false)
                    {
                        IoCompleted(this, e);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Close(SocketCloseReason.SocketError);
                }
            }
            else
            {
                Close(SocketCloseReason.SocketError);
            }
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
            //MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //MainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            MainSocket.Bind(localEndPoint);

            // start the server with a listen backlog.
            MainSocket.Listen(Config.ListenerBacklog);

            // post accepts on the listening socket
            StartAccept(null);
            _isStopped = false;

            // Invoke the started event.
            Started?.Invoke(this, EventArgs.Empty);
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

            // If we are at max sessions, close the new connection with a connection refused reason.
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
                ((ISocketSession)session).Start();

                // Invoke the events.
                OnConnect(session);
            }

            // Accept the next connection request
            StartAccept(e);

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
            _isStopped = true;

            // Invoke the stopped event.
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        void ITransportLayer.Send(byte[] buffer, int start, int count)
        {
            Send(buffer, start, count);
        }
    }
}
