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
	}
}
