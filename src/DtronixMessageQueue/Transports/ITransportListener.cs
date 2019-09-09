using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportListener
    {

        /// <summary>
        /// Fired when a new client connects.
        /// </summary>
        event EventHandler<TransportSessionEventArgs> Connected;

        /// <summary>
        /// Fired when a connected client disconnects.
        /// </summary>
        event EventHandler<TransportSessionEventArgs> Disconnected;

        /// <summary>
        /// Event invoked when the server has stopped listening for connections and has shut down.
        /// </summary>
        event EventHandler Stopped;

        /// <summary>
        /// Event invoked when the server has started listening for incoming connections.
        /// </summary>
        event EventHandler Started;

        /// <summary>
        /// True if the server is listening and accepting connections.  False if the server is closed.
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Starts the lister listening for new incoming connections.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the listener from accepting new connections.
        /// </summary>
        void Stop();
    }
}
