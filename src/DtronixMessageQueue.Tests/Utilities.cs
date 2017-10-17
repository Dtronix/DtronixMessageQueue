using System;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests
{
    static class Utilities
    {
        public static void CompareFrame(MqFrame expected, MqFrame actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            Assert.AreEqual(expected.FrameType, actual.FrameType);
            Assert.AreEqual(expected.Buffer, actual.Buffer);
        }

        public static byte[] SequentialBytes(int len)
        {
            var number = 0;
            byte[] val = new byte[len];

            for (int i = 0; i < len; i++)
            {
                val[i] = (byte) number++;
                if (number > 255)
                {
                    number = 0;
                }
            }

            return val;
        }
    }
}