using System.Security.Cryptography;

namespace DtronixMessageQueue.TcpSocket
{
    /// <summary>
    /// Class to implement on classes which have setup events.
    /// </summary>
    public interface ISetupSocketSession
    {
        /// <summary>
        /// Start the session's secure negotiation.
        /// </summary>
        void SecureSession(RSACng rsa);
    }
}