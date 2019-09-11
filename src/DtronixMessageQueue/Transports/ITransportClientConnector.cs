using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    interface ITransportClientConnector : IClientConnector
    {
        Action<ISession> SessionCreated { get; set; }
    }
}
