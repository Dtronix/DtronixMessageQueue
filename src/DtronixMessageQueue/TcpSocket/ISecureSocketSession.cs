using System.Security.Cryptography;

namespace DtronixMessageQueue.TcpSocket
{
    public interface ISecureSocketSession
    {
        /// <summary>
        /// Start the session's secure negotiation.
        /// </summary>
        void SecureSession(RSACng rsa);
    }
}