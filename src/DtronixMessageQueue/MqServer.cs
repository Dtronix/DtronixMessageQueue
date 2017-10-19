using System;
using System.Threading;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Message queue server to handle incoming clients
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class MqServer<TSession, TConfig> : MqSessionHandler<TSession, TConfig>
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
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
        /// Initializes a new instance of a message queue.
        /// </summary>
        public MqServer(TConfig config) : base(config, TransportLayerMode.Server)
        {
            TransportLayer.StateChanged += (sender, args) =>
            {
                switch (args.State)
                {
                    case TransportLayerState.Started:
                        Started?.Invoke(this, EventArgs.Empty);
                        break;

                    case TransportLayerState.Stopped:
                        Stopped?.Invoke(this, EventArgs.Empty);
                        break;

                }

            };
        }


        /// <summary>
        /// Starts the server and begins listening for incoming connections.
        /// </summary>
        public void Start()
        {
            TransportLayer.Start();
        }


        protected override void OnConnected(TSession session)
        {
            base.OnConnected(session);

            TransportLayer.AcceptAsync();
        }

        /// <summary>
        /// Terminates this server and notify all connected clients.
        /// </summary>
        public void Stop()
        {
            TSession[] sessions = new TSession[ConnectedSessions.Values.Count];
            ConnectedSessions.Values.CopyTo(sessions, 0);

            // Close all connected sessions.
            foreach (var session in sessions)
            {
                if (session.State == TransportLayerState.Connected)
                    session.Close(SessionCloseReason.Closing);
            }

            TransportLayer.Stop();
        }
    }
}