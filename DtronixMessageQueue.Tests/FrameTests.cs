using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DtronixMessageQueue.Tests {
	public class FrameTests {
		private MqFrame actual_frame;
		private byte[] actual_bytes;
		private byte[] expected_bytes;

		public FrameTests() {

		}

		[Fact]
		public void Frame_creates_empty_frame() {
			expected_bytes = new byte[] { 1 };
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
			expected_bytes = new byte[] { 1 };
			actual_frame = new MqFrame(new byte[] {}, MqFrameType.Empty);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_creates_empty_last_frame() {
			expected_bytes = new byte[] { 4 };
			actual_frame = new MqFrame(null, MqFrameType.EmptyLast);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_empty_last_frame_throws_on_bytes() {
			Assert.Throws<ArgumentException>(() => new MqFrame(new byte[] { 1 }, MqFrameType.EmptyLast));
		}

		[Fact]
		public void Frame_creates_more_frame_bytes() {
			expected_bytes = new byte[] { 2, 5, 0, 1, 2, 3, 4, 5 };
			actual_frame = new MqFrame(new byte[] { 1, 2, 3, 4, 5 }, MqFrameType.More);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_creates_last_frame_bytes() {
			expected_bytes = new byte[] { 3, 5, 0, 1, 2, 3, 4, 5 };
			actual_frame = new MqFrame(new byte[] { 1, 2, 3, 4, 5 }, MqFrameType.Last);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_full() {
			expected_bytes = new byte[] {3, 5, 0, 1, 2, 3, 4, 241};
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(0, new byte[] {1,2,3,4,241}, 0, 5);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_offset() {
			expected_bytes = new byte[] { 3, 3, 0, 3, 4, 241 };
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(0, new byte[] { 1, 2, 3, 4, 241 }, 2, 3);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_offset_length() {
			expected_bytes = new byte[] { 3, 2, 0, 3, 4 };
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, new byte[] { 1, 2, 3, 4, 241 }, 2, 2);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_array_offset_length_position() {
			expected_bytes = new byte[] { 3, 3, 0, 0, 3, 4 };
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, new byte[] { 1, 2, 3, 4, 241 }, 2, 2);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_bool_true() {
			expected_bytes = new byte[] { 3, 1, 0, 1 };
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, true);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_bool_false() {
			expected_bytes = new byte[] { 3, 2, 0, 0, 0 };
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
		public void Frame_writes_byte() {
			expected_bytes = new byte[] { 3, 1, 0, 231 };
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, (byte)231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_byte_position() {
			expected_bytes = new byte[] { 3, 2, 0, 0, 231 };
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(1, (byte)231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_short_positive() {
			expected_bytes = new byte[] { 3, 2, 0, 93, 94};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, (short)24157);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_short_negative() {
			expected_bytes = new byte[] { 3, 2, 0, 163, 161 };
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, (short)-24157);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_short_position() {
			expected_bytes = new byte[] { 3, 3, 0, 0, 93, 94 };
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, (short)24157);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ushort() {
			expected_bytes = new byte[] {3, 2, 0, 191, 215};
			actual_frame = new MqFrame(new byte[2], MqFrameType.Last);
			actual_frame.Write(0, (ushort)55231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ushort_position() {
			expected_bytes = new byte[] { 3, 3, 0, 0, 191, 215 };
			actual_frame = new MqFrame(new byte[3], MqFrameType.Last);
			actual_frame.Write(1, (ushort)55231);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_int_positive() {
			expected_bytes = new byte[] { 3, 4, 0, 210, 2, 150, 73 };
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (int)1234567890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_int_negative() {
			expected_bytes = new byte[] { 3, 4, 0, 46, 253, 105, 182 };
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (int)-1234567890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_int_position() {
			expected_bytes = new byte[] { 3, 5, 0, 0, 210, 2, 150, 73 };
			actual_frame = new MqFrame(new byte[5], MqFrameType.Last);
			actual_frame.Write(1, (int)1234567890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_uint() {
			expected_bytes = new byte[] {3, 4, 0, 167, 251, 4, 253};
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (uint)4244962215);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}


		[Fact]
		public void Frame_writes_uint_position() {
			expected_bytes = new byte[] { 3, 4, 0, 167, 251, 4, 253 };
			actual_frame = new MqFrame(new byte[4], MqFrameType.Last);
			actual_frame.Write(0, (uint)4244962215);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}


		[Fact]
		public void Frame_writes_long_positive() {
			expected_bytes = new byte[] {3, 8, 0, 178, 125, 244, 181, 59, 233, 33, 17};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (long)1234524215541267890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_long_negative() {
			expected_bytes = new byte[] {3, 8, 0, 78, 130, 11, 74, 196, 22, 222, 238};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (long)-1234524215541267890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_long_position() {
			expected_bytes = new byte[] { 3, 9, 0, 0, 178, 125, 244, 181, 59, 233, 33, 17 };
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, (long)1234524215541267890);
			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ulong() {
			expected_bytes = new byte[] { 3, 8, 0, 63, 244, 163, 154, 134, 47, 214, 251 };
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (ulong)18146744003702551615);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_ulong_position() {
			expected_bytes = new byte[] { 3, 9, 0, 0, 63, 244, 163, 154, 134, 47, 214, 251 };
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, (ulong)18146744003702551615);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_double() {
			expected_bytes = new byte[] {3, 8, 0, 119, 219, 133, 230, 214, 28, 200, 64};
			actual_frame = new MqFrame(new byte[8], MqFrameType.Last);
			actual_frame.Write(0, (double)12345.67891);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_double_position() {
			expected_bytes = new byte[] {3, 9, 0, 0, 119, 219, 133, 230, 214, 28, 200, 64};
			actual_frame = new MqFrame(new byte[9], MqFrameType.Last);
			actual_frame.Write(1, (double)12345.67891);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_float() {
			expected_bytes = new byte[] {3, 4, 0, 121, 233, 246, 66};
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
		public void Frame_writes_decimal() {
			expected_bytes = new byte[] {3, 16, 0, 160, 107, 84, 143, 156, 7, 157, 126, 0, 0, 0, 0, 0, 0, 0, 0};
			actual_frame = new MqFrame(new byte[16], MqFrameType.Last);
			actual_frame.Write(0, (decimal)9123456789123456789.9123456789123456789);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_decimal_position() {
			expected_bytes = new byte[] { 3, 17, 0, 0, 160, 107, 84, 143, 156, 7, 157, 126, 0, 0, 0, 0, 0, 0, 0, 0 };
			actual_frame = new MqFrame(new byte[17], MqFrameType.Last);
			actual_frame.Write(1, (decimal)9123456789123456789.9123456789123456789);

			actual_bytes = actual_frame.RawFrame();

			Assert.Equal(expected_bytes, actual_bytes);
		}

		[Fact]
		public void Frame_writes_sbyte_positive() {
			expected_bytes = new byte[] { 3, 1, 0, 101 };
			actual_frame = new MqFrame(new byte[1], MqFrameType.Last);
			actual_frame.Write(0, (sbyte) 101);

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
	}
}
