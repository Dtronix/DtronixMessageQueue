using System;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportClientConnector
    {
        /// <summary>
        /// Fired when the client establishes a successful connection to a server.
        /// </summary>
        event EventHandler<TransportSessionEventArgs> Connected;

        /// <summary>
        /// Fired when the connecting client fails to connect to the server.
        /// </summary>
        event EventHandler ConnectionError;

        /// <summary>
        /// True if the this client connector is connecting or connected to a server.  False otherwise.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Connects to server.
        /// </summary>
        void Connect();
    }
}