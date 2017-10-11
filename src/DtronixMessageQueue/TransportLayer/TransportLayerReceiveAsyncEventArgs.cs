using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TransportLayer
{
    public class TransportLayerReceiveAsyncEventArgs : EventArgs, IDisposable
    {
        public byte[] Buffer { get; set; }
        public void Dispose()
        {
        }
    }
}
