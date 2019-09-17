using System;
using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Transparent
{
    public class TransparentLayerSession : ApplicationSession
    {
        private readonly ApplicationConfig _config;

        public TransparentLayerSession(ITransportSession transportSession, ApplicationConfig config)
        :base(transportSession)
        {
            _config = config;
        }

    }
}
