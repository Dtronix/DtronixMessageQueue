using System;
using DtronixMessageQueue.Layers;

namespace DtronixMessageQueue
{
    public interface IClientConnector
    {
        /// <summary>
        /// Fired when the client establishes a successful connection to a server.
        /// </summary>
        Action<ISession> Connected { get; set; }

        /// <summary>
        /// Fired when the connecting client fails to connect to the server.
        /// </summary>
        Action ConnectionError { get; set; }
        
        ISession Session { get; }

        /// <summary>
        /// Connects to server.
        /// </summary>
        void Connect();
    }
}