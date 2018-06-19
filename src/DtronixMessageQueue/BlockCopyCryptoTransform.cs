using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Class used for straight BlockCOpy of input buffer to output buffer.
    /// Used for sessions which have not yet negotiated the encryption channels.
    /// </summary>
    internal class BlockCopyCryptoTransform : ICryptoTransform
    {
        public int InputBlockSize => 16;
        public int OutputBlockSize => 16;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => true;

        /// <summary>
        /// Copies the specified region of the input byte array to the specified region of the output byte array.
        /// </summary>
        /// <returns>
        /// The number of bytes written.
        /// </returns>
        /// <param name="inputBuffer">The input for which to compute the transform. </param>
        /// <param name="inputOffset">The offset into the input byte array from which to begin using data. </param>
        /// <param name="inputCount">The number of bytes in the input byte array to use as data. </param>
        /// <param name="outputBuffer">The output to which to write the transform. </param>
        /// <param name="outputOffset">The offset into the output byte array from which to begin writing data. </param>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {

            Buffer.BlockCopy(inputBuffer,inputOffset, outputBuffer, outputOffset, inputCount);
            return inputCount;
        }

        /// <summary>
        /// Copies the specified region of the specified byte array and returns it.
        /// </summary>
        /// <returns>
        /// The computed transform.
        /// </returns>
        /// <param name="inputBuffer">The input for which to compute the transform. </param>
        /// <param name="inputOffset">The offset into the byte array from which to begin using data. </param>
        /// <param name="inputCount">The number of bytes in the byte array to use as data. </param>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, 0, inputCount);
            return outputBuffer;
        }

        public void Dispose()
        {
        }
    }
}
