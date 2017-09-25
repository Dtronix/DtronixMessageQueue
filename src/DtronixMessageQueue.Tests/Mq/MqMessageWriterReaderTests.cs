using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqMessageWriterReaderTests
    {
        public ITestOutputHelper Output;
        private MqMessageWriter _messageBuilder;
        private MqMessageReader _messageReader;
        private MqConfig _config = new MqConfig();

        private const string FillerText =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        public MqMessageWriterReaderTests(ITestOutputHelper output)
        {
            Output = output;
            _messageBuilder = new MqMessageWriter(_config);
            _messageReader = new MqMessageReader();
        }

        [Fact]
        public void MessageWriter_writes_bool_true()
        {
            var expectedValue = (bool) true;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadBoolean());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_bool_false()
        {
            var expectedValue = (bool) false;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadBoolean());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_byte()
        {
            var expectedValue = (byte) 221;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_sbyte_positive()
        {
            var expectedValue = (sbyte) 101;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadSByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_sbyte_negative()
        {
            var expectedValue = (sbyte) -101;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadSByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_short_positive()
        {
            var expectedValue = (short) 21457;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadInt16());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_short_negative()
        {
            var expectedValue = (short) -21457;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadInt16());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_ushort()
        {
            var expectedValue = (ushort) 51574;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadUInt16());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_int_positive()
        {
            var expectedValue = (int) 515725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadInt32());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_int_negative()
        {
            var expectedValue = (int) -515725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadInt32());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_uint()
        {
            var expectedValue = (uint) 1215725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadUInt32());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_long_positive()
        {
            var expectedValue = (long) 515352135236725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadInt64());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_long_negative()
        {
            var expectedValue = (long) -515352135236725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadInt64());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_ulong()
        {
            var expectedValue = (ulong) 12231512365365725234;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadUInt64());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_float()
        {
            var expectedValue = (float) 123.456;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadSingle());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_double()
        {
            var expectedValue = (double) 12345.67891;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadDouble());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_decimal()
        {
            var expectedValue = (decimal) 9123456789123456789.9123456789123456789;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadDecimal());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
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

            Assert.Equal(expectedValue.Length, byteArraySize);

            var resultByteArray = new byte[byteArraySize];
            var position = 0;
            foreach (var frame in message)
            {
                Buffer.BlockCopy(frame.Buffer, 0, resultByteArray, position, frame.DataLength);
                position += frame.DataLength;
            }

            Assert.Equal(expectedValue, resultByteArray);
        }

        [Fact]
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

        [Fact]
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

            Assert.Equal(expectedValue.Length, read);
            Assert.Equal(expectedValue, actualValue);
            Assert.True(_messageReader.IsAtEnd);
        }


        [Fact]
        public void MessageWriter_writes_string()
        {
            var expectedValue = FillerText;
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadString());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Fact]
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

            Assert.Equal(expectedValue, _messageReader.ReadString());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Fact]
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

            var expectedGuid = Guid.NewGuid();
            _messageBuilder.Write((Guid)expectedGuid);

            var message = _messageBuilder.ToMessage();

            _messageReader.Message = message;

            Assert.Equal(true, _messageReader.ReadBoolean());
            Assert.Equal(false, _messageReader.ReadBoolean());

            Assert.Equal('D', _messageReader.ReadChar());
            Assert.Equal(new char[] {'A', 'Y', 'X', '0', '9', '8'}, _messageReader.ReadChars(6));


            Assert.Equal((byte) 214, _messageReader.ReadByte());
            Assert.Equal((sbyte) 125, _messageReader.ReadSByte());
            Assert.Equal((sbyte) -125, _messageReader.ReadSByte());

            Assert.Equal((short) 4513, _messageReader.ReadInt16());
            Assert.Equal((short) -4513, _messageReader.ReadInt16());
            Assert.Equal((ushort) 43513, _messageReader.ReadUInt16());

            Assert.Equal((int) 236236231, _messageReader.ReadInt32());
            Assert.Equal((int) -236236231, _messageReader.ReadInt32());
            Assert.Equal((uint) 2362326231, _messageReader.ReadUInt32());

            Assert.Equal((long) 2362362312561531, _messageReader.ReadInt64());
            Assert.Equal((long) -2362362312561531, _messageReader.ReadInt64());
            Assert.Equal((ulong) 2362362312561531125, _messageReader.ReadUInt64());

            Assert.Equal((float) 1234.56789, _messageReader.ReadSingle());
            Assert.Equal((double) 123467.5678912, _messageReader.ReadDouble());
            Assert.Equal((decimal) 123456789123456789.123456789123456789, _messageReader.ReadDecimal());

            var readByteArray = new byte[50];
            _messageReader.Read(readByteArray, 0, readByteArray.Length);
            Assert.Equal(expectedByteArray, readByteArray);

            Assert.Equal(FillerText, _messageReader.ReadString());

            Assert.Equal(expectedGuid, _messageReader.ReadGuid());

            Assert.True(_messageReader.IsAtEnd);
        }


        [Fact]
        public void MessageWriter_writes_char()
        {
            var expectedValue = (char) 'D';
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadChar());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_char_array()
        {
            var expectedValue = new char[] {'A', 'B', 'C', '1', '2', '3'};
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadChars(expectedValue.Length));
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageWriter_writes_char_array_slice()
        {
            var inputValue = new char[] {'A', 'B', 'C', '1', '2', '3'};
            var expectedValue = new char[] {'B', 'C', '1', '2'};
            _messageBuilder.Write(inputValue, 1, 4);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadChars(expectedValue.Length));
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_peeks_char()
        {
            var expectedValue = new char[] {'D', 'Z'};
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue[0], _messageReader.PeekChar());
            Assert.Equal(expectedValue, _messageReader.ReadChars(expectedValue.Length));
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_reads_to_end()
        {
            var expectedValue = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_reads_to_end_multi_frame()
        {
            var expectedValue = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};

            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5});
            _messageBuilder.FinalizeFrame();

            _messageBuilder.Write(new byte[] {6, 7, 8, 9, 0});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_reads_to_end_multi_frame_skipping_empty_frame()
        {
            var expectedValue = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};

            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.FinalizeFrame();

            _messageBuilder.Write(new byte[] {6, 7, 8, 9, 0});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Fact]
        public void MessageReader_reads_to_end_partial()
        {
            var expectedValue = new byte[] {4, 5, 6, 7, 8, 9, 10};
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.ReadBytes(3);
            Assert.Equal(expectedValue, _messageReader.ReadToEnd());
            Assert.True(_messageReader.IsAtEnd);
        }


        [Fact]
        public void MessageReader_maintains_position()
        {
            var expectedValue = 5;
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.ReadBytes(5);
            Assert.Equal(expectedValue, _messageReader.Position);
            Assert.False(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_maintains_position_across_frames()
        {
            var expectedValue = 7;
            _messageBuilder.Write(new byte[] {1, 2, 3, 4});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.Write(new byte[] {5, 6, 7, 8, 9, 10});

            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;


            Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7}, _messageReader.ReadBytes(7));
            Assert.Equal(expectedValue, _messageReader.Position);
            Assert.False(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_throws_when_reading_simple_type_across_frames()
        {
            _messageBuilder.Write(new byte[] {1, 2});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.Write(new byte[] {5, 6});

            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Throws<InvalidOperationException>(() => _messageReader.ReadInt32());
        }

        [Fact]
        public void MessageReader_skips_bytes()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.Skip(4);

            Assert.Equal(4, _messageReader.Position);
            Assert.False(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_skips_empty_frames()
        {
            _messageBuilder.Write(new byte[] {1, 2});
            _messageBuilder.FinalizeFrame();
            _messageBuilder.FinalizeFrame();
            _messageBuilder.Write(new byte[] {3, 4});

            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.Skip(3);


            Assert.Equal(3, _messageReader.Position);
            Assert.Equal(4, _messageReader.ReadByte());
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
        public void MessageReader_sets_position()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            _messageReader.Position = 2;
            Assert.Equal(2, _messageReader.Position);

            _messageReader.Position = 1;
            Assert.Equal(1, _messageReader.Position);

            _messageReader.Position = 3;
            Assert.Equal(3, _messageReader.Position);
        }

        [Fact]
        public void MessageReader_sets_position_and_reads()
        {
            _messageBuilder.Write(new byte[] {1, 2, 3, 4, 5});
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;


            Assert.Equal(new byte[] {1, 2, 3, 4, 5}, _messageReader.ReadBytes(5));
            Assert.Equal(5, _messageReader.Position);


            _messageReader.Position = 2;
            Assert.Equal(2, _messageReader.Position);

            Assert.Equal(new byte[] {3, 4, 5}, _messageReader.ReadBytes(3));
            Assert.Equal(5, _messageReader.Position);
            Assert.True(_messageReader.IsAtEnd);
        }

        [Fact]
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

        [Fact]
        public void MessageReader_writes_guid()
        {
            var expectedValue = (Guid)Guid.NewGuid();
            _messageBuilder.Write(expectedValue);
            var message = _messageBuilder.ToMessage();
            _messageReader.Message = message;

            Assert.Equal(expectedValue, _messageReader.ReadGuid());
            Assert.True(_messageReader.IsAtEnd);
        }
    }
}