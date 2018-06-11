using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.TcpSocket
{
    public abstract class EncryptedSocketSession<TSession, TConfig> : TcpSocketSession<TSession, TConfig>, ISecureSocketSession
        where TSession : TcpSocketSession<TSession, TConfig>, new()
        where TConfig : TcpSocketConfig
    {

        private MemoryQueueStream _negotiationStream;

        private byte[] _sendBuffer = new byte[16];
        private int _sendBufferLength = 0;

        private byte[] _receiveTransformedBuffer;
        private byte[] _receivePartialBuffer = new byte[16];
        private int _receivePartialBufferLength = 0;

        private RSACng _rsa;

        /// <summary>
        /// Contains the encryption and description methods.
        /// </summary>
        private Aes _aes;

        private ICryptoTransform _decryptor;

        private ICryptoTransform _encryptor;


        protected EncryptedSocketSession()
        {
            _negotiationStream = new MemoryQueueStream();
        }

        protected override void OnSetup()
        {
            _receiveTransformedBuffer = new byte[Config.SendAndReceiveBufferSize];
        }

        /// <summary>
        /// Start the session's receive events.
        /// </summary>
        void ISecureSocketSession.SecureSession(RSACng rsa)
        {
            if (CurrentState != State.Closed)
                return;

            ((ISetupSocketSession) this).StartSession();

            if (SocketHandler.Mode != TcpSocketMode.Server)
                return;

            // Send the protocol version number along with the public key to the connected client.
            var versionPublicKey = new byte[513];
            versionPublicKey[0] = ProtocolVersion;
            Buffer.BlockCopy(SocketHandler.RsaPublicKey, 0, versionPublicKey, 1, 512);

            Send(versionPublicKey, 0, versionPublicKey.Length);
        }


        private void SecureConnectionReceive(byte[] buffer)
        {
            if (CurrentState == State.Closed)
                return;

            _negotiationStream.Write(buffer);

            if (SocketHandler.Mode == TcpSocketMode.Client)
            {
                // Set the connection version number.
                if (ConnectionProtocolVersion == 0)
                {
                    var version = new byte[1];
                    _negotiationStream.Read(version, 0, 1);
                    //ConnectionProtocolVersion = version[0];
                }

                // 4096 bits for the public key
                if (ConnectionProtocolVersion > 0
                    && SocketHandler.RsaPublicKey == null
                    && _negotiationStream.Length >= 512)
                {
                    SocketHandler.RsaPublicKey = new byte[512];
                    _negotiationStream.Read(SocketHandler.RsaPublicKey, 0, 512);

                    _rsa = new RSACng();


                    _rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = SocketHandler.RsaPublicKey,
                        Exponent = new byte[] { 1, 0, 1 }
                    });

                    // Setup the 256 AES encryption
                    _aes = new AesCryptoServiceProvider
                    {
                        KeySize = 256,
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    };

                    // Server side.
                    _aes.GenerateKey();
                    _aes.GenerateIV();

                    var ivKey = new byte[48];

                    // Copy the arrays into the a structure to be read by the server.
                    Buffer.BlockCopy(_aes.IV, 0, ivKey, 0, 16);
                    Buffer.BlockCopy(_aes.Key, 0, ivKey, 16, 32);

                    var rsaIvKey = _rsa.Encrypt(ivKey, RSAEncryptionPadding.OaepSHA512);

                    _rsa.Dispose();
                    _rsa = null;

                    // Send the encryption key.
                    Send(rsaIvKey, 0, rsaIvKey.Length);

                    SecureConnectionComplete();
                }
            }
            else // Server
            {

                if (_aes == null
                    && _negotiationStream.Length >= 512)
                {
                    var rsaIvKey = new byte[512];
                    _negotiationStream.Read(rsaIvKey, 0, rsaIvKey.Length);
                    var ivKey = _rsa.Decrypt(rsaIvKey, RSAEncryptionPadding.OaepSHA512);

                    var iv = new byte[16];
                    var key = new byte[32];

                    Buffer.BlockCopy(ivKey, 0, iv, 0, 16);
                    Buffer.BlockCopy(ivKey, 16, key, 0, 32);

                    // Setup the 256 AES encryption
                    _aes = new AesCryptoServiceProvider
                    {
                        KeySize = 256,
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7,
                        Key = key,
                        IV = iv
                    };

                    SecureConnectionComplete();
                }
            }
        }

        private void SecureConnectionComplete()
        {
            // If there is left over data in the stream, send it on to the 
            if (_negotiationStream.Length > 0)
            {
                var excessBuffer = new byte[_negotiationStream.Length];
                _negotiationStream.Read(excessBuffer, 0, excessBuffer.Length);

                HandleIncomingBytes(excessBuffer);
            }

            // Set the encryptor and decryptor.
            _decryptor = _aes.CreateDecryptor(); //new PlainCryptoTransform(); 
            _encryptor = _aes.CreateEncryptor(); // new PlainCryptoTransform(); 

            _negotiationStream.Close();
            _negotiationStream = null;

            // Set the state to connected.
            CurrentState = State.Connected;
            OnConnected();
        }


        private int TransformWithDataBuffer(byte[] bufferSource, int offsetSource, int lengthSource, byte[] bufferDest, int offsetDest, byte[] transformBuffer, ref int transformBufferLength, ICryptoTransform transformer, bool preDebug, bool postDebug)
        {
            int transformLength = 0;
            if (transformBufferLength > 0)
            {
                int bufferTaken = Math.Min(lengthSource, 16 - transformBufferLength);
                Buffer.BlockCopy(bufferSource, offsetSource, transformBuffer, transformBufferLength, bufferTaken);
                transformBufferLength += bufferTaken;
                offsetSource += bufferTaken;
                lengthSource -= bufferTaken;
            }

            if (transformBufferLength == 16)
            {
                transformLength += 16;
                transformer.TransformBlock(transformBuffer, 0, transformBufferLength, bufferDest, offsetDest);
                if (preDebug)
                {
                    WriteBuffer("Pre 16 Byte Buffer", transformBuffer, 0, transformBufferLength);
                }

                if (postDebug)
                {
                    WriteBuffer("Post 16 Byte Buffer", bufferDest, offsetDest, 16);
                }
            }
            else if (lengthSource == 0)
            {
                return 0;
            }

            // Get the size of the block in chucks of 16 bytes.
            int blockTransformLength = lengthSource / 16 * 16;
            int transformRemain = lengthSource - blockTransformLength;

            if (preDebug && blockTransformLength > 0)
            {
                WriteBuffer("Pre block transform", bufferSource, offsetSource, blockTransformLength);
            }

            // Only transform if we have a block large enough.
            if (blockTransformLength > 0)
                transformLength += transformer.TransformBlock(bufferSource, offsetSource, blockTransformLength, bufferDest,
                    offsetDest + transformBufferLength);

            if (postDebug && blockTransformLength > 0)
            {
                WriteBuffer("Post block transform", bufferDest, offsetDest + transformBufferLength, blockTransformLength);
            }

            // If the buffer was used this round, reset it to zero.
            if (transformBufferLength == 16)
                transformBufferLength = 0;


            if (postDebug)
            {
                WriteBuffer("Whole buffer", bufferDest, offsetDest, transformLength);
            }

            if (transformRemain > 0)
            {
                Buffer.BlockCopy(bufferSource, offsetSource + blockTransformLength, transformBuffer, 0, transformRemain);
                transformBufferLength = transformRemain;
            }

            return transformLength;
        }


        private void WriteBuffer(string header, byte[] buffer, int offset, int length)
        {
            var sb = new StringBuilder(header + " new byte[" + length + "] { ");
            for (var i = 0; i < length; i++)
            {
                sb.Append(buffer[i + offset]).Append(" ");
            }
            sb.AppendLine("}");
            Console.WriteLine(sb.ToString());
        }

        private enum EncryptedMessageType : byte
        {
            Unknown = 0,
            FullMessage = 1,
            PartialMessage = 2,
        }

        protected override void Send(byte[] buffer, int offset, int length, bool last)
        {
            base.Send(buffer, offset, length, last);
        }

  
    }
}
