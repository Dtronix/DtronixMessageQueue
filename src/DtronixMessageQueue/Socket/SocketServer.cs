using System;
using System.Net;
using System.Net.Sockets;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue.Socket
{
    /// <summary>
    /// Base functionality for handling connection requests.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class SocketServer<TSession, TConfig> : SessionHandler<TSession, TConfig>
        where TSession : SocketSession<TSession, TConfig>, new()
        where TConfig : TransportLayerConfig
    {
        /// <summary>
        /// Event invoked when the server has stopped listening for connections and has shut down.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Event invoked when the server has started listening for incoming connections.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Creates a socket server with the specified configurations.
        /// </summary>
        /// <param name="config">Configurations for this socket.</param>
        public SocketServer(TConfig config, ITransportLayer transportLayer) : base(config, transportLayer)
        {
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