using System.Security.Cryptography;

namespace DtronixMessageQueue.TlsSocket
{
    /// <summary>
    /// Class to implement on classes which have setup events.
    /// </summary>
    public interface ISetupSocketSession
    {
        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void Start();

        /// <summary>
        /// Called before the session is formally started to encrypt all communications.
        /// </summary>
        /// <param name="rsa">Rsa used for key exchange.  Null if the connection is being made on the client side.</param>
        void SecureSession(RSACng rsa);
    }
}