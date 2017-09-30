using System;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Message queue server to handle incoming clients
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class MqServer<TSession, TConfig> : SocketServer<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Event fired when a new message arrives at a session's mailbox.
        /// </summary>
        public event EventHandler<IncomingMessageEventArgs<TSession, TConfig>> IncomingMessage;

        /// <summary>
        /// Event invoked when the server has stopped listening for connections and has shut down.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Event invoked when the server has started listening for incoming connections.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Initializes a new instance of a message queue.
        /// </summary>
        public MqServer(TConfig config, ITransportLayer transportLayer) : base(config)
        {
            Setup();
        }


        /// <summary>
        /// Creates a session with the specified socket.
        /// </summary>
        /// <param name="sessionSocket">Socket to associate with the session.</param>
        protected override TSession CreateSession(System.Net.Sockets.Socket sessionSocket)
        {
            var session = base.CreateSession(sessionSocket);
            session.IncomingMessage += OnIncomingMessage;

            return session;
        }

        /// <summary>
        /// Event method invoker
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event object containing the mailbox to retrieve the message from.</param>
        private void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e)
        {
            IncomingMessage?.Invoke(sender, e);
        }

        /// <summary>
        /// Called internally with a session on the server closes.
        /// </summary>
        /// <param name="session">Session which closed.</param>
        /// <param name="reason">Reason the session closed.</param>
        protected override void OnClose(TSession session, SessionCloseReason reason)
        {
            session.IncomingMessage -= OnIncomingMessage;
            session.Dispose();
            base.OnClose(session, reason);
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {


            TransportLayer.Listen();

            TransportLayer.AcceptConnection();

            // Invoke the started event.
            Started?.Invoke(this, EventArgs.Empty);
        }



        /// <summary>
        /// Event called to remove the disconnected session from the list of active connections.
        /// </summary>
        /// <param name="sender">Sender of the disconnection event.</param>
        /// <param name="e">Session events.</param>
        private void RemoveClientEvent(object sender, SessionCloseEventArgs<TSession, TConfig> e)
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
                session.Close(SessionCloseReason.ServerClosing);
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
    }
}