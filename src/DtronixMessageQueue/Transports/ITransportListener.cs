using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports
{
    public interface ITransportListener : IListener
    {
        Action<ISession> SessionCreated { get; set; }

    }
}
