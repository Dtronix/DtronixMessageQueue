using System;
using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Transparent
{
    public class TransparentApplicationSession : ApplicationSession
    {
        private readonly ApplicationConfig _config;

        public TransparentApplicationSession(ITransportSession transportSession, ApplicationConfig config)
        :base(transportSession, config)
        {
            _config = config;
        }

    }
}
