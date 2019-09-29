using System;

namespace DtronixMessageQueue.Layers
{
    public interface IClientConnector
    {
        /// <summary>
        /// Fired when the client establishes a successful connection to a server.
        /// </summary>
        event EventHandler<SessionEventArgs> Connected;

        /// <summary>
        /// Fired when the connecting client fails to connect to the server.
        /// </summary>
        event EventHandler ConnectionError;
        
        ISession Session { get; }

        /// <summary>
        /// Connects to server.
        /// </summary>
        void Connect();
    }
}