using System;
using System.Text;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqMessageWriterReaderTests
    {

        private MqMessageWriter _messageBuilder;
        private MqMessageReader _messageReader;
        private MqConfig _config = new MqConfig();

        private const string FillerText =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public MqMessageWriterReaderTests()
        {
            _messageBuilder = new MqMessageWriter(_config);
            _messageReader = new MqMessageReader();
        }

        [Test]
        public void MessageWriter_writes_bool_true()
        {
            var expectedValue = (bool) true;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadBoolean());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_bool_false()
        {
            var expectedValue = (bool) false;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadBoolean());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_byte()
        {
            var expectedValue = (byte) 221;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_sbyte_positive()
        {
            var expectedValue = (sbyte) 101;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadSByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_sbyte_negative()
        {
            var expectedValue = (sbyte) -101;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadSByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_short_positive()
        {
            var expectedValue = (short) 21457;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadInt16());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_short_negative()
        {
            var expectedValue = (short) -21457;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadInt16());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_ushort()
        {
            var expectedValue = (ushort) 51574;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadUInt16());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_int_positive()
        {
            var expectedValue = (int) 515725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadInt32());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_int_negative()
        {
            var expectedValue = (int) -515725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadInt32());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_uint()
        {
            var expectedValue = (uint) 1215725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadUInt32());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_long_positive()
        {
            var expectedValue = (long) 515352135236725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadInt64());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_long_negative()
        {
            var expectedValue = (long) -515352135236725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadInt64());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_ulong()
        {
            var expectedValue = (ulong) 12231512365365725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadUInt64());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_float()
        {
            var expectedValue = (float) 123.456;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadSingle());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_double()
        {
            var expectedValue = (double) 12345.67891;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadDouble());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_decimal()
        {
            var expectedValue = (decimal) 9123456789123456789.9123456789123456789;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadDecimal());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_multi_frame_byte_array()
        {
            var expectedValue = new byte[1024 * 32];
            var number = 0;
            for (int i = 0; i < 1024 * 32; i++)
            {
                if (number == 255)
                {
                    number = 0;
                }

                expectedValue[i] = (byte) number++;
            }
            _messageBuilder.Write(expectedValue, 0, expectedValue.Length);
            var message = _messageBuilder.ToMessage();
            VerifyMessageBytes(expectedValue, message);
        }

        private void VerifyMessageBytes(byte[] expectedValue, MqMessage message)
        {
            var byteArraySize = 0;

            foreach (var frame in message)
            {
                byteArraySize += frame.DataLength;
            }

            Assert.AreEqual(expectedValue.Length, byteArraySize);

            var resultByteArray = new byte[byteArraySize];
            var position = 0;
            foreach (var frame in message)
            {
                Buffer.BlockCopy(frame.Buffer, 0, resultByteArray, position, frame.DataLength);
                position += frame.DataLength;
            }

            Assert.AreEqual(expectedValue, resultByteArray);
        }

        [Test]
        public void MessageWriter_writes_multi_frame_string_bytes()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < 100; i++)
            {
                sb.Append(FillerText);
            }

            var expectedValue = sb.ToString();
            var stringBytes = Encoding.UTF8.GetBytes(expectedValue);
            var expectedBytes = new byte[stringBytes.Length + 4];
            var intBytes = BitConverter.GetBytes(stringBytes.Length);
            Buffer.BlockCopy(intBytes, 0, expectedBytes, 0, 4);

            Buffer.BlockCopy(stringBytes, 0, expectedBytes, 4, stringBytes.Length);

            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            VerifyMessageBytes(expectedBytes, message);
        }

        [Test]
        public void MessageReader_reads_multi_frame_byte_array()
        {
            var expectedValue = new byte[1024 * 32];
            var number = 0;
            for (int i = 0; i < 1024 * 32; i++)
            {
                if (number == 255)
                {
                    number = 0;
                }

                expectedValue[i] = (byte) number++;
            }
            _messageBuilder.Write(expectedValue, 0, expectedValue.Length);
            var message = _messageBuilder.ToMessage();
            var actualValue = new byte[expectedValue.Length];
            _messageReader.Message = message;

            var read = _messageReader.Read(actualValue, 0, actualValue.Length);

            Assert.AreEqual(expectedValue.Length, read);
            Assert.AreEqual(expectedValue, actualValue);
            Assert.True(_messageReader.IsAtEnd);
        }


        [Test]
        public void MessageWriter_writes_string()
        {
            var expectedValue = FillerText;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadString());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Test]
        public void MessageReader_reads_multi_frame_string()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < 100; i++)
            {
                sb.Append(FillerText);
            }

            var expectedValue = sb.ToString();
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadString());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Test]
        public void MessageWriter_multiple_reads_writes()
        {
            _messageBuilder.Write(true);
            _messageBuilder.Write(false);

            _messageBuilder.Write((char) 'D');
            _messageBuilder.Write(new char[] {'A', 'Y', 'X', '0', '9', '8'});

            _messageBuilder.Write((byte) 214);
            _messageBuilder.Write((sbyte) 125);
            _messageBuilder.Write((sbyte) -125);

            _messageBuilder.Write((short) 4513);
            _messageBuilder.Write((short) -4513);
            _messageBuilder.Write((ushort) 43513);

            _messageBuilder.Write((int) 236236231);
            _messageBuilder.Write((int) -236236231);
            _messageBuilder.Write((uint) 2362326231);

            _messageBuilder.Write((long) 2362362312561531);
            _messageBuilder.Write((long) -2362362312561531);
            _messageBuilder.Write((ulong) 2362362312561531125);

            _messageBuilder.Write((float) 1234.56789);
            _messageBuilder.Write((double) 123467.5678912);
            _messageBuilder.Write((decimal) 123456789123456789.123456789123456789);

            var expectedByteArray = Utilities.SequentialBytes(50);
            _messageBuilder.Write(expectedByteArray, 0, expectedByteArray.Length);

            _messageBuilder.Write(FillerText);

            var message = _messageBuilder.ToMessage();

            _messageReader.Message = message;

            Assert.AreEqual(true, _messageReader.ReadBoolean());
            Assert.AreEqual(false, _messageReader.ReadBoolean());

            Assert.AreEqual('D', _messageReader.ReadChar());
            Assert.AreEqual(new char[] {'A', 'Y', 'X', '0', '9', '8'}, _messageReader.ReadChars(6));


            Assert.AreEqual((byte) 214, _messageReader.ReadByte());
            Assert.AreEqual((sbyte) 125, _messageReader.ReadSByte());
            Assert.AreEqual((sbyte) -125, _messageReader.ReadSByte());

            Assert.AreEqual((short) 4513, _messageReader.ReadInt16());
            Assert.AreEqual((short) -4513, _messageReader.ReadInt16());
            Assert.AreEqual((ushort) 43513, _messageReader.ReadUInt16());

            Assert.AreEqual((int) 236236231, _messageReader.ReadInt32());
            Assert.AreEqual((int) -236236231, _messageReader.ReadInt32());
            Assert.AreEqual((uint) 2362326231, _messageReader.ReadUInt32());

            Assert.AreEqual((long) 2362362312561531, _messageReader.ReadInt64());
            Assert.AreEqual((long) -2362362312561531, _messageReader.ReadInt64());
            Assert.AreEqual((ulong) 2362362312561531125, _messageReader.ReadUInt64());

            Assert.AreEqual((float) 1234.56789, _messageReader.ReadSingle());
            Assert.AreEqual((double) 123467.5678912, _messageReader.ReadDouble());
            Assert.AreEqual((decimal) 123456789123456789.123456789123456789, _messageReader.ReadDecimal());

            var readByteArray = new byte[50];
            _messageReader.Read(readByteArray, 0, readByteArray.Length);
            Assert.AreEqual(expectedByteArray, readByteArray);

            Assert.AreEqual(FillerText, _messageReader.ReadString());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Test]
        public void MessageWriter_writes_char()
        {
            var expectedValue = (char) 'D';
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadChar());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_char_array()
        {
            var expectedValue = new char[] {'A', 'B', 'C', '1', '2', '3'};
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadChars(expectedValue.Length));
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageWriter_writes_char_array_slice()
        {
            var inputValue = new char[] {'A', 'B', 'C', '1', '2', '3'};
            var expectedValue = new char[] {'B', 'C', '1', '2'};
            _messageBuilder.Write(inputValue, 1, 4);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadChars(expectedValue.Length));
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_peeks_char()
        {
            var expectedValue = new char[] {'D', 'Z'};
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue[0], _messageReader.PeekChar());
            Assert.AreEqual(expectedValue, _messageReader.ReadChars(expectedValue.Length));
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_reads_to_end()
        {
            var expectedValue = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_reads_to_end_multi_frame()
        {
            var expectedValue = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};

            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5});
            _messageBuilder.FinalizeFrame();

            _messageBuilder.Write(new byte[] {6, 7, 8, 9, 0});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_reads_to_end_multi_frame_skipping_empty_frame()
        {
            var expectedValue = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};

            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.FinalizeFrame();

            _messageBuilder.Write(new byte[] {6, 7, 8, 9, 0});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.AreEqual(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Test]
        public void MessageReader_reads_to_end_partial()
        {
            var expectedValue = new byte[] {4, 5, 6, 7, 8, 9, 10};
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.ReadBytes(3);
            Assert.AreEqual(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Test]
        public void MessageReader_maintains_position()
        {
            var expectedValue = 5;
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.ReadBytes(5);
            Assert.AreEqual(expectedValue, _messageReader.Position);
            Assert.False(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_maintains_position_across_frames()
        {
            var expectedValue = 7;
            _messageBuilder.Write(new byte[] {1, 2, 3, 4});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.Write(new byte[] {5, 6, 7, 8, 9, 10});

            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;


            Assert.AreEqual(new byte[] {1, 2, 3, 4, 5, 6, 7}, _messageReader.ReadBytes(7));
            Assert.AreEqual(expectedValue, _messageReader.Position);
            Assert.False(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_throws_when_reading_simple_type_across_frames()
        {
            _messageBuilder.Write(new byte[] {1, 2});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.Write(new byte[] {5, 6});

            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Throws<InvalidOperationException>(() => _messageReader.ReadInt32());
        }

        [Test]
        public void MessageReader_skips_bytes()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.Skip(4);

            Assert.AreEqual(4, _messageReader.Position);
            Assert.False(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_skips_empty_frames()
        {
            _messageBuilder.Write(new byte[] {1, 2});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.FinalizeFrame();
            _messageBuilder.Write(new byte[] {3, 4});

            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.Skip(3);


            Assert.AreEqual(3, _messageReader.Position);
            Assert.AreEqual(4, _messageReader.ReadByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_sets_position()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.Position = 2;
            Assert.AreEqual(2, _messageReader.Position);

            _messageReader.Position = 1;
            Assert.AreEqual(1, _messageReader.Position);

            _messageReader.Position = 3;
            Assert.AreEqual(3, _messageReader.Position);
        }

        [Test]
        public void MessageReader_sets_position_and_reads()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;


            Assert.AreEqual(new byte[] {1, 2, 3, 4, 5}, _messageReader.ReadBytes(5));
            Assert.AreEqual(5, _messageReader.Position);


            _messageReader.Position = 2;
            Assert.AreEqual(2, _messageReader.Position);

            Assert.AreEqual(new byte[] {3, 4, 5}, _messageReader.ReadBytes(3));
            Assert.AreEqual(5, _messageReader.Position);
            Assert.True(_messageReader.IsAtEnd);
        }

        [Test]
        public void MessageReader_sets_position_and_updates_isatend()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.False(_messageReader.IsAtEnd);

            _messageReader.Position = 9;
            Assert.True(_messageReader.IsAtEnd);

            _messageReader.Position = 8;
            Assert.False(_messageReader.IsAtEnd);
        }
    }
}