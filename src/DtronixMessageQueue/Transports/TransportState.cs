using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public enum TransportState : byte
    {
        /// <summary>
        /// State has not been set.
        /// </summary>
        Unknown,

        /// <summary>
        /// Session has connected to remote session.
        /// </summary>
        Connected,

        /// <summary>
        /// Session has been closed and no longer can be used.
        /// </summary>
        Closed,

        /// <summary>
        /// Socket is in an error state.
        /// </summary>
        Error,
    }
}
