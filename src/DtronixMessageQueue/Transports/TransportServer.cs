using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportListener
    {
        event EventHandler<TransportSessionEventArgs> Connected;
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

        void Start();
        void Stop();
    }
}
