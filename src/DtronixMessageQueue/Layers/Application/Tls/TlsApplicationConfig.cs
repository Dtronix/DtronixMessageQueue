using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsApplicationConfig : ApplicationConfig
    {
        public X509Certificate Certificate { get; set; }

        public int AuthTimeout { get; set; } = 5000;

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        public int TlsSchedulerThreads { get; set; } = -1;
    }
}