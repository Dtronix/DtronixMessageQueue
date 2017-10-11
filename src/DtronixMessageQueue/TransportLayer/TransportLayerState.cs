using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    /// <summary>
    /// Current state of the socket.
    /// </summary>
    public enum TransportLayerState : byte
    {
        /// <summary>
        /// State has not been set.
        /// </summary>
        Unknown,

        Starting,

        Started,

        Stopping,

        Stopped,

        /// <summary>
        /// Session is attempting to connect to remote connection.
        /// </summary>
        Connecting,

        /// <summary>
        /// Session has connected to remote session.
        /// </summary>
        Connected,

        /// <summary>
        /// Session is in the process of closing its connection.
        /// </summary>
        Closing,

        /// <summary>
        /// Session has been closed and no longer can be used.
        /// </summary>
        Closed,

        /// <summary>
        /// Socket is in an error state.
        /// </summary>
        Error
    }
}
