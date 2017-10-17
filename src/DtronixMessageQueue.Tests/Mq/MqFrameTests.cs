using System;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Mq
{
    [TestFixture]
    public class MqFrameTests
    {
        private MqFrame _actualFrame;
        private byte[] _actualBytes;
        private byte[] _expectedBytes;
        MqConfig _config = new MqConfig();

        public MqFrameTests()
        {
        }

        [SetUp]
        public void Init()
        {
            
        }

       [Test]
        public void Frame_creates_empty_frame()
        {
            _expectedBytes = new byte[] {1};
            _actualFrame = new MqFrame(null, MqFrameType.Empty, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_empty_frame_throws_on_bytes()
        {
            Assert.Throws<ArgumentException>(() => new MqFrame(new byte[] {1}, MqFrameType.Empty, _config));
        }

       [Test]
        public void Frame_empty_frame_accepts_empty_array()
        {
            _expectedBytes = new byte[] {1};
            _actualFrame = new MqFrame(new byte[] {}, MqFrameType.Empty, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_creates_empty_last_frame()
        {
            _expectedBytes = new byte[] {4};
            _actualFrame = new MqFrame(null, MqFrameType.EmptyLast, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_creates_command_frame()
        {
            _expectedBytes = new byte[] {5, 1, 0, 1};
            _actualFrame = new MqFrame(new byte[] {1}, MqFrameType.Command, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_creates_ping_frame()
        {
            _expectedBytes = new byte[] {6};
            _actualFrame = new MqFrame(null, MqFrameType.Ping, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_empty_last_frame_throws_on_bytes()
        {
            Assert.Throws<ArgumentException>(() => new MqFrame(new byte[] {1}, MqFrameType.EmptyLast, _config));
        }

       [Test]
        public void Frame_creates_more_frame_bytes()
        {
            _expectedBytes = new byte[] {2, 5, 0, 1, 2, 3, 4, 5};
            _actualFrame = new MqFrame(new byte[] {1, 2, 3, 4, 5}, MqFrameType.More, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_creates_last_frame_bytes()
        {
            _expectedBytes = new byte[] {3, 5, 0, 1, 2, 3, 4, 5};
            _actualFrame = new MqFrame(new byte[] {1, 2, 3, 4, 5}, MqFrameType.Last, _config);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_byte_array_full()
        {
            _expectedBytes = new byte[] {3, 5, 0, 1, 2, 3, 4, 241};
            _actualFrame = new MqFrame(new byte[5], MqFrameType.Last, _config);
            _actualFrame.Write(0, new byte[] {1, 2, 3, 4, 241}, 0, 5);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_byte_array_offset()
        {
            _expectedBytes = new byte[] {3, 3, 0, 3, 4, 241};
            _actualFrame = new MqFrame(new byte[3], MqFrameType.Last, _config);
            _actualFrame.Write(0, new byte[] {1, 2, 3, 4, 241}, 2, 3);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_byte_array_offset_length()
        {
            _expectedBytes = new byte[] {3, 2, 0, 3, 4};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, new byte[] {1, 2, 3, 4, 241}, 2, 2);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_byte_array_offset_length_position()
        {
            _expectedBytes = new byte[] {3, 3, 0, 0, 3, 4};
            _actualFrame = new MqFrame(new byte[3], MqFrameType.Last, _config);
            _actualFrame.Write(1, new byte[] {1, 2, 3, 4, 241}, 2, 2);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_byte_array()
        {
            _expectedBytes = new byte[] {1, 2, 3, 4, 5};
            _actualFrame = new MqFrame(_expectedBytes, MqFrameType.Last, _config);
            _actualBytes = new byte[5];
            _actualFrame.Read(0, _actualBytes, 0, 5);

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_bool_true()
        {
            _expectedBytes = new byte[] {3, 1, 0, 1};
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, true);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_bool_false()
        {
            _expectedBytes = new byte[] {3, 2, 0, 0, 0};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, false);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_bool_position()
        {
            _expectedBytes = new byte[] {3, 2, 0, 0, 1};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(1, true);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_bool()
        {
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, true);

            Assert.AreEqual(true, _actualFrame.ReadBoolean(0));
        }

       [Test]
        public void Frame_writes_byte()
        {
            _expectedBytes = new byte[] {3, 1, 0, 231};
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, (byte) 231);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_byte_position()
        {
            _expectedBytes = new byte[] {3, 2, 0, 0, 231};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(1, (byte) 231);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_sbyte_negative()
        {
            _expectedBytes = new byte[] {3, 1, 0, 155};
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, (sbyte) -101);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_sbyte_position()
        {
            _expectedBytes = new byte[] {3, 2, 0, 0, 101};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(1, (sbyte) 101);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_sbyte()
        {
            var value = (sbyte) 101;
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadSByte(0));
        }

       [Test]
        public void Frame_reads_sbyte_position()
        {
            var value = (sbyte) 101;
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadSByte(1));
        }


       [Test]
        public void Frame_writes_char()
        {
            _expectedBytes = new byte[] {3, 1, 0, 68};
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, (char) 'D');

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_char_position()
        {
            _expectedBytes = new byte[] {3, 2, 0, 0, 68};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(1, (char) 'D');

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_char()
        {
            var value = (char) 'D';
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadChar(0));
        }

       [Test]
        public void Frame_reads_char_position()
        {
            var value = (char) 'D';
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadChar(1));
        }

       [Test]
        public void Frame_writes_short_positive()
        {
            _expectedBytes = new byte[] {3, 2, 0, 93, 94};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, (short) 24157);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_short_negative()
        {
            _expectedBytes = new byte[] {3, 2, 0, 163, 161};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, (short) -24157);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_short_position()
        {
            _expectedBytes = new byte[] {3, 3, 0, 0, 93, 94};
            _actualFrame = new MqFrame(new byte[3], MqFrameType.Last, _config);
            _actualFrame.Write(1, (short) 24157);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }


       [Test]
        public void Frame_reads_short()
        {
            var value = (short) 24157;
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadInt16(0));
        }

       [Test]
        public void Frame_reads_short_position()
        {
            var value = (short) 24157;
            _actualFrame = new MqFrame(new byte[3], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadInt16(1));
        }

       [Test]
        public void Frame_writes_ushort()
        {
            _expectedBytes = new byte[] {3, 2, 0, 191, 215};
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, (ushort) 55231);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_ushort_position()
        {
            _expectedBytes = new byte[] {3, 3, 0, 0, 191, 215};
            _actualFrame = new MqFrame(new byte[3], MqFrameType.Last, _config);
            _actualFrame.Write(1, (ushort) 55231);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_ushort()
        {
            var value = (ushort) 55231;
            _actualFrame = new MqFrame(new byte[2], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadUInt16(0));
        }

       [Test]
        public void Frame_reads_ushort_position()
        {
            var value = (ushort) 55231;
            _actualFrame = new MqFrame(new byte[3], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadUInt16(1));
        }


       [Test]
        public void Frame_writes_int_positive()
        {
            _expectedBytes = new byte[] {3, 4, 0, 210, 2, 150, 73};
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, (int) 1234567890);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_int_negative()
        {
            _expectedBytes = new byte[] {3, 4, 0, 46, 253, 105, 182};
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, (int) -1234567890);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_int_position()
        {
            _expectedBytes = new byte[] {3, 5, 0, 0, 210, 2, 150, 73};
            _actualFrame = new MqFrame(new byte[5], MqFrameType.Last, _config);
            _actualFrame.Write(1, (int) 1234567890);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_int()
        {
            var value = (int) 1234567890;
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadInt32(0));
        }

       [Test]
        public void Frame_reads_int_position()
        {
            var value = (int) 1234567890;
            _actualFrame = new MqFrame(new byte[5], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadInt32(1));
        }

       [Test]
        public void Frame_writes_uint()
        {
            _expectedBytes = new byte[] {3, 4, 0, 167, 251, 4, 253};
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, (uint) 4244962215);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }


       [Test]
        public void Frame_writes_uint_position()
        {
            _expectedBytes = new byte[] {3, 4, 0, 167, 251, 4, 253};
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, (uint) 4244962215);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_uint()
        {
            var value = (uint) 4244962215;
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadUInt32(0));
        }

       [Test]
        public void Frame_reads_uint_position()
        {
            var value = (uint) 4244962215;
            _actualFrame = new MqFrame(new byte[5], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadUInt32(1));
        }


       [Test]
        public void Frame_writes_long_positive()
        {
            _expectedBytes = new byte[] {3, 8, 0, 178, 125, 244, 181, 59, 233, 33, 17};
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, (long) 1234524215541267890);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_long_negative()
        {
            _expectedBytes = new byte[] {3, 8, 0, 78, 130, 11, 74, 196, 22, 222, 238};
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, (long) -1234524215541267890);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_long_position()
        {
            _expectedBytes = new byte[] {3, 9, 0, 0, 178, 125, 244, 181, 59, 233, 33, 17};
            _actualFrame = new MqFrame(new byte[9], MqFrameType.Last, _config);
            _actualFrame.Write(1, (long) 1234524215541267890);
            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_long()
        {
            var value = (long) 4244962215;
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadInt64(0));
        }

       [Test]
        public void Frame_reads_long_position()
        {
            var value = (long) 4244962215;
            _actualFrame = new MqFrame(new byte[9], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadInt64(1));
        }

       [Test]
        public void Frame_writes_ulong()
        {
            _expectedBytes = new byte[] {3, 8, 0, 63, 244, 163, 154, 134, 47, 214, 251};
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, (ulong) 18146744003702551615);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_ulong_position()
        {
            _expectedBytes = new byte[] {3, 9, 0, 0, 63, 244, 163, 154, 134, 47, 214, 251};
            _actualFrame = new MqFrame(new byte[9], MqFrameType.Last, _config);
            _actualFrame.Write(1, (ulong) 18146744003702551615);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_ulong()
        {
            var value = (ulong) 18146744003702551615;
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadUInt64(0));
        }

       [Test]
        public void Frame_reads_ulong_position()
        {
            var value = (ulong) 18146744003702551615;
            _actualFrame = new MqFrame(new byte[9], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadUInt64(1));
        }


       [Test]
        public void Frame_writes_float()
        {
            _expectedBytes = new byte[] {3, 4, 0, 121, 233, 246, 66};
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, (float) 123.456);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_float_position()
        {
            _expectedBytes = new byte[] {3, 5, 0, 0, 121, 233, 246, 66};
            _actualFrame = new MqFrame(new byte[5], MqFrameType.Last, _config);
            _actualFrame.Write(1, (float) 123.456);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_float()
        {
            var value = (float) 123.456;
            _actualFrame = new MqFrame(new byte[4], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadSingle(0));
        }

       [Test]
        public void Frame_reads_float_position()
        {
            var value = (float) 123.456;
            _actualFrame = new MqFrame(new byte[5], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadSingle(1));
        }

       [Test]
        public void Frame_writes_double()
        {
            _expectedBytes = new byte[] {3, 8, 0, 119, 219, 133, 230, 214, 28, 200, 64};
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, (double) 12345.67891);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_double_position()
        {
            _expectedBytes = new byte[] {3, 9, 0, 0, 119, 219, 133, 230, 214, 28, 200, 64};
            _actualFrame = new MqFrame(new byte[9], MqFrameType.Last, _config);
            _actualFrame.Write(1, (double) 12345.67891);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_double()
        {
            var value = (double) 12345.67891;
            _actualFrame = new MqFrame(new byte[8], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadDouble(0));
        }

       [Test]
        public void Frame_reads_double_position()
        {
            var value = (double) 12345.67891;
            _actualFrame = new MqFrame(new byte[9], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadDouble(1));
        }


       [Test]
        public void Frame_writes_decimal()
        {
            _expectedBytes = new byte[] {3, 16, 0, 160, 107, 84, 143, 156, 7, 157, 126, 0, 0, 0, 0, 0, 0, 0, 0};
            _actualFrame = new MqFrame(new byte[16], MqFrameType.Last, _config);
            _actualFrame.Write(0, (decimal) 9123456789123456789.9123456789123456789);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_decimal_position()
        {
            _expectedBytes = new byte[] {3, 17, 0, 0, 160, 107, 84, 143, 156, 7, 157, 126, 0, 0, 0, 0, 0, 0, 0, 0};
            _actualFrame = new MqFrame(new byte[17], MqFrameType.Last, _config);
            _actualFrame.Write(1, (decimal) 9123456789123456789.9123456789123456789);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_decimal()
        {
            var value = (decimal) 9123456789123456789.9123456789123456789;
            _actualFrame = new MqFrame(new byte[16], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadDecimal(0));
        }

       [Test]
        public void Frame_reads_decimal_position()
        {
            var value = (decimal) 9123456789123456789.9123456789123456789;
            _actualFrame = new MqFrame(new byte[17], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadDecimal(1));
        }

       [Test]
        public void Frame_writes_sbyte_positive()
        {
            _expectedBytes = new byte[] {3, 1, 0, 101};
            _actualFrame = new MqFrame(new byte[1], MqFrameType.Last, _config);
            _actualFrame.Write(0, (sbyte) 101);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_ascii_text_prepended_with_size()
        {
            _expectedBytes = new byte[]
            {
                3, 28, 0,
                26, 0,
                97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
                117, 118, 119, 120, 121, 122
            };
            _actualFrame = new MqFrame(new byte[28], MqFrameType.Last, _config);
            _actualFrame.WriteAscii(0, "abcdefghijklmnopqrstuvwxyz", true);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_ascii_text_prepended_with_size_position()
        {
            _expectedBytes = new byte[]
            {
                3, 29, 0,
                0,
                26, 0,
                97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
                117, 118, 119, 120, 121, 122
            };
            _actualFrame = new MqFrame(new byte[29], MqFrameType.Last, _config);
            _actualFrame.WriteAscii(1, "abcdefghijklmnopqrstuvwxyz", true);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_ascii_text_not_prepended_with_size()
        {
            _expectedBytes = new byte[]
            {
                3, 26, 0,
                97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
                117, 118, 119, 120, 121, 122
            };
            _actualFrame = new MqFrame(new byte[26], MqFrameType.Last, _config);
            _actualFrame.WriteAscii(0, "abcdefghijklmnopqrstuvwxyz", false);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_ascii_text_not_prepended_with_size_position()
        {
            _expectedBytes = new byte[]
            {
                3, 27, 0,
                0,
                97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
                117, 118, 119, 120, 121, 122
            };
            _actualFrame = new MqFrame(new byte[27], MqFrameType.Last, _config);
            _actualFrame.WriteAscii(1, "abcdefghijklmnopqrstuvwxyz", false);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_ascii_text_prepended_with_size()
        {
            var value = "abcdefghijklmnopqrstuvwxyz";
            _actualFrame = new MqFrame(new byte[28], MqFrameType.Last, _config);
            _actualFrame.WriteAscii(0, value, true);

            Assert.AreEqual(value, _actualFrame.ReadAscii(0));
        }

       [Test]
        public void Frame_reads_ascii_text_not_prepended_with_size()
        {
            var value = "abcdefghijklmnopqrstuvwxyz";
            _actualFrame = new MqFrame(new byte[26], MqFrameType.Last, _config);
            _actualFrame.WriteAscii(0, value, false);

            Assert.AreEqual(value, _actualFrame.ReadAscii(0, _actualFrame.DataLength));
        }


       [Test]
        public void Frame_throws_on_ascii_text_write_when_larger_than_frame()
        {
            var value = "abcdefghijklmnopqrstuvwxyz";

            for (int i = 0; i < 10; i++)
            {
                value += value;
            }
            _actualFrame = new MqFrame(new byte[_config.FrameBufferSize], MqFrameType.Last, _config);

            Assert.Throws<InvalidOperationException>(() => _actualFrame.WriteAscii(0, value, false));
        }

       [Test]
        public void Frame_writes_guid()
        {
            var value = Guid.NewGuid();

            _expectedBytes = new byte[] { 3, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            Buffer.BlockCopy(value.ToByteArray(), 0, _expectedBytes, 3, 16);

            _actualFrame = new MqFrame(new byte[17], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_writes_guid_position()
        {
            var value = Guid.NewGuid();

            _expectedBytes = new byte[] {3, 18, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

            Buffer.BlockCopy(value.ToByteArray(), 0, _expectedBytes, 4, 16);

            _actualFrame = new MqFrame(new byte[18], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            _actualBytes = _actualFrame.RawFrame();

            Assert.AreEqual(_expectedBytes, _actualBytes);
        }

       [Test]
        public void Frame_reads_guid()
        {
            var value = Guid.NewGuid();
            _actualFrame = new MqFrame(new byte[16], MqFrameType.Last, _config);
            _actualFrame.Write(0, value);

            Assert.AreEqual(value, _actualFrame.ReadGuid(0));
        }

       [Test]
        public void Frame_reads_guid_position()
        {
            var value = Guid.NewGuid();
            _actualFrame = new MqFrame(new byte[17], MqFrameType.Last, _config);
            _actualFrame.Write(1, value);

            Assert.AreEqual(value, _actualFrame.ReadGuid(1));
        }
    }
}