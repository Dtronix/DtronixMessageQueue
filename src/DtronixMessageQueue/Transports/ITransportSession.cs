using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportSession : ISession
    {
        void Connect();

        /// <summary>
        /// Contains the session which is 
        /// </summary>
        ISession WrapperSession { get; set; }
    }
}
