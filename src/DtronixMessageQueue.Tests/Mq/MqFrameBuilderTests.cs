using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DtronixMessageQueue.Tests.Mq {
	public class MqFrameBuilderTests {
		private MqFrame empty_last_frame;
		private MqFrame empty_frame;
		private MqFrame last_frame;
		private MqFrame more_frame;
		private MqFrameBuilder frame_builder;
		private MqConfig config = new MqConfig();
		private MqFrame command_frame;
		private MqFrame ping_frame;

		public MqFrameBuilderTests() {

			frame_builder = new MqFrameBuilder(config);
			empty_last_frame = new MqFrame(null, MqFrameType.EmptyLast, config);
			empty_frame = new MqFrame(null, MqFrameType.Empty, config);
			last_frame = new MqFrame(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, MqFrameType.Last, config);
			more_frame = new MqFrame(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, MqFrameType.More, config);
			command_frame = new MqFrame(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, MqFrameType.Command, config);
			ping_frame = new MqFrame(null, MqFrameType.Ping, config);
		}

		[Fact]
		public void FrameBuilder_parses_empty_frame() {
			frame_builder.Write(empty_frame.RawFrame(), 0, empty_frame.FrameSize);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Utilities.CompareFrame(empty_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_parses_empty_last_frame() {
			frame_builder.Write(empty_last_frame.RawFrame(), 0, empty_frame.FrameSize);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Utilities.CompareFrame(empty_last_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_parses_last_frame() {
			frame_builder.Write(last_frame.RawFrame(), 0, last_frame.FrameSize);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Utilities.CompareFrame(last_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_parses_more_frame() {
			frame_builder.Write(more_frame.RawFrame(), 0, more_frame.FrameSize);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Utilities.CompareFrame(more_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_parses_command_frame() {
			frame_builder.Write(command_frame.RawFrame(), 0, command_frame.FrameSize);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Utilities.CompareFrame(command_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_parses_ping_frame() {
			frame_builder.Write(ping_frame.RawFrame(), 0, ping_frame.FrameSize);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Utilities.CompareFrame(ping_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_parses_multiple_frames() {
			frame_builder.Write(more_frame.RawFrame(), 0, more_frame.FrameSize);
			frame_builder.Write(more_frame.RawFrame(), 0, more_frame.FrameSize);

			while (frame_builder.Frames.Count > 0) {
				var parsed_frame = frame_builder.Frames.Dequeue();
				Utilities.CompareFrame(more_frame, parsed_frame);
			}
		}

		[Fact]
		public void FrameBuilder_parses_frames_in_parts() {
			var frame_bytes = last_frame.RawFrame();

			for (int i = 0; i < frame_bytes.Length; i++) {
				frame_builder.Write(new [] { frame_bytes[i] }, 0, 1);
			}

			var parsed_frame = frame_builder.Frames.Dequeue();
			Utilities.CompareFrame(last_frame, parsed_frame);
		}

		[Fact]
		public void FrameBuilder_throws_passed_buffer_too_large() {
			Assert.Throws<InvalidDataException>(() => {
				frame_builder.Write(new byte[config.FrameBufferSize + 1], 0, config.FrameBufferSize + 1);
			});
		}

		[Fact]
		public void FrameBuilder_throws_frame_zero_length() {
			Assert.Throws<InvalidDataException>(() => {
				frame_builder.Write(new byte[] {2, 0, 0, 1}, 0, 4);
			});
		}

		[Fact]
		public void FrameBuilder_throws_frame_specified_length_too_large() {
			Assert.Throws<InvalidDataException>(() => {
				frame_builder.Write(new byte[] { 2, 255, 255, 1 }, 0, 4);
			});
		}

		[Fact]
		public void FrameBuilder_throws_frame_type_out_of_range() {
			var max_type_enum = Enum.GetValues(typeof(MqFrameType)).Cast<byte>().Max() + 1;

			Assert.Throws<InvalidDataException>(() => {
				frame_builder.Write(new byte[] { (byte)max_type_enum }, 0, 1);
			});
		}

		[Fact]
		public void FrameBuilder_parsed_frame_data() {
			frame_builder.Write(new byte[] { 2, 10, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, 0, 13);
			var parsed_frame = frame_builder.Frames.Dequeue();

			Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, parsed_frame.Buffer);
		}

	}
}
