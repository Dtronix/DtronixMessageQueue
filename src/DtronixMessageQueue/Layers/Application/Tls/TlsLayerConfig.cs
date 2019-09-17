using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsLayerConfig : ApplicationConfig
    {
        public X509Certificate Certificate { get; set; } 

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }
    }
}