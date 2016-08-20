using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DtronixMessageQueue.Tests {
	public class MqFrameTests {
		private MqFrame actual_frame;
		private byte[] actual_bytes;
		private byte[] expected_bytes;

		public MqFrameTests() {

		}

		[Fact]
		public void Frame_creates_empty_frame() {
			expected_bytes = new byte[] {1};
			actual_frame = new MqFrame(null, MqFrameType.Empty);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_empty_frame_throws_on_bytes() {
			Assert.Throws<ArgumentException>(() => new MqFrame(new byte[] {1}, MqFrameType.Empty));
		}

		[Fact]
		public void Frame_empty_frame_accepts_empty_array() {
			expected_bytes = new byte[] {1};
			actual_frame = new MqFrame(new byte[] {}, MqFrameType.Empty);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_creates_empty_last_frame() {
			expected_bytes = new byte[] {4};
			actual_frame = new MqFrame(null, MqFrameType.EmptyLast);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_empty_last_frame_throws_on_bytes() {
			Assert.Throws<ArgumentException>(() => new MqFrame(new byte[] {1}, MqFrameType.EmptyLast));
		}

		[Fact]
		public void Frame_creates_more_frame_bytes() {
			expected_bytes = new byte[] {2, 5, 0, 1, 2, 3, 4, 5};
			actual_frame = new MqFrame(new byte[] {1, 2, 3, 4, 5}, MqFrameType.More);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_creates_last_frame_bytes() {
			expected_bytes = new byte[] {3, 5, 0, 1, 2, 3, 4, 5};
			actual_frame = new MqFrame(new byte[] {1, 2, 3, 4, 5}, MqFrameType.Last);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_full() {
			expected_bytes = new byte[] {3, 5, 0, 1, 2, 3, 4, 241};
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(0, new byte[] {1, 2, 3, 4, 241}, 0, 5);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_offset() {
			expected_bytes = new byte[] {3, 3, 0, 3, 4, 241};
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(0, new byte[] {1, 2, 3, 4, 241}, 2, 3);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_offset_length() {
			expected_bytes = new byte[] {3, 2, 0, 3, 4};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, new byte[] {1, 2, 3, 4, 241}, 2, 2);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_offset_length_position() {
			expected_bytes = new byte[] {3, 3, 0, 0, 3, 4};
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, new byte[] {1, 2, 3, 4, 241}, 2, 2);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_byte_array() {
			expected_bytes = new byte[] {1, 2, 3, 4, 5};
			actual_frame = new MqFrame(expected_bytes, MqFrameType.Last);
			actual_bytes = new byte[5];
			actual_frame.Read(0, actual_bytes, 0, 5);

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_bool_true() {
			expected_bytes = new byte[] {3, 1, 0, 1};
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, true);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_bool_false() {
			expected_bytes = new byte[] {3, 2, 0, 0, 0};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, false);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_bool_position() {
			expected_bytes = new byte[] {3, 2, 0, 0, 1};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, true);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_bool() {
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, true);
			
			Assert.Equal(true, actual_frame.ReadBoolean(0));
		}

		[Fact]
		public void Frame_writes_byte() {
			expected_bytes = new byte[] {3, 1, 0, 231};
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, (byte) 231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_position() {
			expected_bytes = new byte[] {3, 2, 0, 0, 231};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, (byte) 231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_sbyte_negative() {
			expected_bytes = new byte[] { 3, 1, 0, 155 };
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, (sbyte)-101);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_sbyte_position() {
			expected_bytes = new byte[] { 3, 2, 0, 0, 101 };
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, (sbyte)101);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_sbyte() {
			var value = (sbyte)101;
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadSByte(0));
		}

		[Fact]
		public void Frame_reads_sbyte_position() {
			var value = (sbyte)101;
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadSByte(1));
		}


		[Fact]
		public void Frame_writes_char() {
			expected_bytes = new byte[] { 3, 1, 0, 68 };
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, (char)'D');

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_char_position() {
			expected_bytes = new byte[] { 3, 2, 0, 0, 68 };
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, (char)'D');

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_char() {
			var value = (char)'D';
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadChar(0));
		}

		[Fact]
		public void Frame_reads_char_position() {
			var value = (char)'D';
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadChar(1));
		}

		[Fact]
		public void Frame_writes_short_positive() {
			expected_bytes = new byte[] {3, 2, 0, 93, 94};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, (short) 24157);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_short_negative() {
			expected_bytes = new byte[] {3, 2, 0, 163, 161};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, (short) -24157);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_short_position() {
			expected_bytes = new byte[] {3, 3, 0, 0, 93, 94};
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, (short) 24157);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}


		[Fact]
		public void Frame_reads_short() {
			var value = (short) 24157;
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadInt16(0));
		}

		[Fact]
		public void Frame_reads_short_position() {
			var value = (short)24157;
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadInt16(1));
		}

		[Fact]
		public void Frame_writes_ushort() {
			expected_bytes = new byte[] {3, 2, 0, 191, 215};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, (ushort) 55231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ushort_position() {
			expected_bytes = new byte[] {3, 3, 0, 0, 191, 215};
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, (ushort) 55231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_ushort() {
			var value = (ushort)55231;
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadUInt16(0));
		}

		[Fact]
		public void Frame_reads_ushort_position() {
			var value = (ushort)55231;
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadUInt16(1));
		}


		[Fact]
		public void Frame_writes_int_positive() {
			expected_bytes = new byte[] {3, 4, 0, 210, 2, 150, 73};
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (int) 1234567890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_int_negative() {
			expected_bytes = new byte[] {3, 4, 0, 46, 253, 105, 182};
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (int) -1234567890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_int_position() {
			expected_bytes = new byte[] {3, 5, 0, 0, 210, 2, 150, 73};
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(1, (int) 1234567890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_int() {
			var value = (int)1234567890;
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadInt32(0));
		}

		[Fact]
		public void Frame_reads_int_position() {
			var value = (int)1234567890;
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadInt32(1));
		}

		[Fact]
		public void Frame_writes_uint() {
			expected_bytes = new byte[] {3, 4, 0, 167, 251, 4, 253};
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (uint) 4244962215);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}


		[Fact]
		public void Frame_writes_uint_position() {
			expected_bytes = new byte[] {3, 4, 0, 167, 251, 4, 253};
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (uint) 4244962215);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_uint() {
			var value = (uint)4244962215;
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadUInt32(0));
		}

		[Fact]
		public void Frame_reads_uint_position() {
			var value = (uint)4244962215;
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadUInt32(1));
		}


		[Fact]
		public void Frame_writes_long_positive() {
			expected_bytes = new byte[] {3, 8, 0, 178, 125, 244, 181, 59, 233, 33, 17};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (long) 1234524215541267890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_long_negative() {
			expected_bytes = new byte[] {3, 8, 0, 78, 130, 11, 74, 196, 22, 222, 238};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (long) -1234524215541267890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_long_position() {
			expected_bytes = new byte[] {3, 9, 0, 0, 178, 125, 244, 181, 59, 233, 33, 17};
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, (long) 1234524215541267890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_long() {
			var value = (long)4244962215;
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadInt64(0));
		}

		[Fact]
		public void Frame_reads_long_position() {
			var value = (long)4244962215;
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadInt64(1));
		}

		[Fact]
		public void Frame_writes_ulong() {
			expected_bytes = new byte[] {3, 8, 0, 63, 244, 163, 154, 134, 47, 214, 251};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (ulong) 18146744003702551615);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ulong_position() {
			expected_bytes = new byte[] {3, 9, 0, 0, 63, 244, 163, 154, 134, 47, 214, 251};
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, (ulong) 18146744003702551615);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_ulong() {
			var value = (ulong)18146744003702551615;
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadUInt64(0));
		}

		[Fact]
		public void Frame_reads_ulong_position() {
			var value = (ulong)18146744003702551615;
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadUInt64(1));
		}


		[Fact]
		public void Frame_writes_float() {
			expected_bytes = new byte[] { 3, 4, 0, 121, 233, 246, 66 };
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (float)123.456);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_float_position() {
			expected_bytes = new byte[] { 3, 5, 0, 0, 121, 233, 246, 66 };
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(1, (float)123.456);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_float() {
			var value = (float)123.456;
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadSingle(0));
		}

		[Fact]
		public void Frame_reads_float_position() {
			var value = (float)123.456;
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadSingle(1));
		}

		[Fact]
		public void Frame_writes_double() {
			expected_bytes = new byte[] {3, 8, 0, 119, 219, 133, 230, 214, 28, 200, 64};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (double) 12345.67891);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_double_position() {
			expected_bytes = new byte[] {3, 9, 0, 0, 119, 219, 133, 230, 214, 28, 200, 64};
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, (double) 12345.67891);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_double() {
			var value = (double)12345.67891;
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadDouble(0));
		}

		[Fact]
		public void Frame_reads_double_position() {
			var value = (double)12345.67891;
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadDouble(1));
		}


		[Fact]
		public void Frame_writes_decimal() {
			expected_bytes = new byte[] {3, 16, 0, 160, 107, 84, 143, 156, 7, 157, 126, 0, 0, 0, 0, 0, 0, 0, 0};
			actual_frame = new MqFrame(new byte[16], MqFrameType.Last);
			actual_frame.Write(0, (decimal) 9123456789123456789.9123456789123456789);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_decimal_position() {
			expected_bytes = new byte[] {3, 17, 0, 0, 160, 107, 84, 143, 156, 7, 157, 126, 0, 0, 0, 0, 0, 0, 0, 0};
			actual_frame = new MqFrame(new byte[17], MqFrameType.Last);
			actual_frame.Write(1, (decimal) 9123456789123456789.9123456789123456789);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_decimal() {
			var value = (decimal)9123456789123456789.9123456789123456789;
			actual_frame = new MqFrame(new byte[16], MqFrameType.Last);
			actual_frame.Write(0, value);

			Assert.Equal(value, actual_frame.ReadDecimal(0));
		}

		[Fact]
		public void Frame_reads_decimal_position() {
			var value = (decimal)9123456789123456789.9123456789123456789;
			actual_frame = new MqFrame(new byte[17], MqFrameType.Last);
			actual_frame.Write(1, value);

			Assert.Equal(value, actual_frame.ReadDecimal(1));
		}

		[Fact]
		public void Frame_writes_sbyte_positive() {
			expected_bytes = new byte[] {3, 1, 0, 101};
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, (sbyte) 101);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ascii_text_prepended_with_size() {
			expected_bytes = new byte[] {
				3, 28, 0, 
				26, 0, 
				97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
				117, 118, 119, 120, 121, 122
			};
			actual_frame = new MqFrame(new byte[28], MqFrameType.Last);
			actual_frame.WriteAscii(0, "abcdefghijklmnopqrstuvwxyz", true);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ascii_text_not_prepended_with_size() {
			expected_bytes = new byte[] {
				3, 26, 0,
				97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
				117, 118, 119, 120, 121, 122
			};
			actual_frame = new MqFrame(new byte[26], MqFrameType.Last);
			actual_frame.WriteAscii(0, "abcdefghijklmnopqrstuvwxyz", false);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_reads_ascii_text_prepended_with_size() {
			var value = "abcdefghijklmnopqrstuvwxyz";
			actual_frame = new MqFrame(new byte[28], MqFrameType.Last);
			actual_frame.WriteAscii(0, value, true);

			Assert.Equal(value, actual_frame.ReadAscii(0));
		}

		[Fact]
		public void Frame_reads_ascii_text_not_prepended_with_size() {
			var value = "abcdefghijklmnopqrstuvwxyz";
			actual_frame = new MqFrame(new byte[26], MqFrameType.Last);
			actual_frame.WriteAscii(0, value, false);

			Assert.Equal(value, actual_frame.ReadAscii(0, actual_frame.DataLength));
		}


		[Fact]
		public void Frame_throws_on_ascii_text_write_when_larger_than_frame() {
			var value = "abcdefghijklmnopqrstuvwxyz";

			for (int i = 0; i < 10; i++) {
				value += value;
			}
			actual_frame = new MqFrame(new byte[MqFrame.MaxFrameSize], MqFrameType.Last);
			
			Assert.Throws<InvalidOperationException>(() => actual_frame.WriteAscii(0, value, false));
		}





	}
}
