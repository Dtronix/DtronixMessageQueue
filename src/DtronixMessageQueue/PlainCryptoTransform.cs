using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    class PlainCryptoTransform : ICryptoTransform
    {
        public void Dispose()
        {
            return;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {

            Buffer.BlockCopy(inputBuffer,inputOffset, outputBuffer, outputOffset, inputCount);
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            throw new NotImplementedException();
        }

        public int InputBlockSize => 16;
        public int OutputBlockSize => 16;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => true;
    }
}
