using System;
using System.Net;
using System.Net.Sockets;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Base functionality for handling connection requests.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class TcpSocketServer<TSession, TConfig> : TcpSocketHandler<TSession, TConfig>
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {
 

        /// <summary>
        /// Creates a socket server with the specified configurations.
        /// </summary>
        /// <param name="config">Configurations for this socket.</param>
        public TcpSocketServer(TConfig config) : base(config, TcpSocketMode.Server)
        {
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {


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
            TSession[] sessions = new TSession[ConnectedSessions.Values.Count];
            ConnectedSessions.Values.CopyTo(sessions, 0);

            foreach (var session in sessions)
            {
                session.Close(CloseReason.Closing);
            }

        }
    }
}