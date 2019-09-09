using System;
using System.Collections.Generic;
using System.Text;

namespace DtronixMessageQueue.Transports.TlsLayer
{
    class TlsLayerSession<T> : T
        where T : TransportSession
    {
    }
}
