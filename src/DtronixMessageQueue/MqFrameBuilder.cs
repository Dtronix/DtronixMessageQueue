using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DtronixMessageQueue {

	/// <summary>
	/// Class to parse raw byte arrays into frames.
	/// </summary>
	public class MqFrameBuilder : IDisposable {

		/// <summary>
		/// Byte buffer used to maintain and parse the passed buffers.
		/// </summary>
		private readonly byte[] internal_buffer;

		/// <summary>
		/// Data for this frame
		/// </summary>
		private byte[] current_frame_data;

		/// <summary>
		/// Current parsed type.  is Unknown by default.
		/// </summary>
		private MqFrameType current_frame_type;

		/// <summary>
		/// Reading position in the internal buffer.  Set by the internal reader.
		/// </summary>
		private int read_position;

		/// <summary>
		/// Writing position in the internal buffer.  Set by the internal writer.
		/// </summary>
		private int write_position;

		/// <summary>
		/// Total length of the internal buffer's data.  Set by the reader and writer.
		/// </summary>
		private int stream_length;

		/// <summary>
		/// Memory stream used to read and write to the internal buffer.
		/// </summary>
		private readonly MemoryStream buffer_stream;

		/// <summary>
		/// Used to cache the maximum size of the MqFrameType.
		/// </summary>
		private static int max_type_enum = -1;

		/// <summary>
		/// Configurations for the connected session.
		/// </summary>
		private readonly MqConfig config;

		/// <summary>
		/// Size in bytes of the header of a frame.
		/// MqFrameType:byte[1], Length:UInt16[2]
		/// </summary>
		public const int HeaderLength = 3;

		/// <summary>
		/// Parsed frames from the incoming stream.
		/// </summary>
		public Queue<MqFrame> Frames { get; } = new Queue<MqFrame>();

		/// <summary>
		/// Creates a new instance of the frame builder to handle parsing of incoming byte stream.
		/// </summary>
		/// <param name="config">Socket configurations for this session.</param>
		public MqFrameBuilder(MqConfig config) {
			this.config = config;
			internal_buffer = new byte[config.FrameBufferSize + MqFrame.HeaderLength];

			// Determine what our max enum value is for the FrameType
			if (max_type_enum == -1) {
				max_type_enum = Enum.GetValues(typeof(MqFrameType)).Cast<byte>().Max();
			}

			buffer_stream = new MemoryStream(internal_buffer, 0, internal_buffer.Length, true, true);
		}

		/// <summary>
		/// Reads from the internal stream.
		/// </summary>
		/// <param name="buffer">Byte buffer to read into.</param>
		/// <param name="offset">Offset position in the buffer to copy from.</param>
		/// <param name="count">Number of bytes to attempt to read.</param>
		/// <returns>Total bytes that were read.</returns>
		private int ReadInternal(byte[] buffer, int offset, int count) {
			buffer_stream.Position = read_position;
			var length = buffer_stream.Read(buffer, offset, count);
			read_position += length;

			// Update the stream length 
			stream_length = write_position - read_position;
			return length;
		}

		/// <summary>
		/// Writes to the internal stream from the specified buffer.
		/// </summary>
		/// <param name="buffer">Buffer to write from.</param>
		/// <param name="offset">Offset position in the buffer copy from.</param>
		/// <param name="count">Number of bytes to copy from the write_buffer.</param>
		private void WriteInternal(byte[] buffer, int offset, int count) {
			buffer_stream.Position = write_position;
			try {
				buffer_stream.Write(buffer, offset, count);
			} catch (Exception e) {
				throw new InvalidDataException("FrameBuilder was sent a frame larger than the session allows.", e);
			}

			write_position += count;

			// Update the stream length 
			stream_length = write_position - read_position;
		}

		/// <summary>
		/// Moves the internal buffer stream from the read position to the end, to the beginning.
		/// Frees up space to write in the buffer.
		/// </summary>
		private void MoveStreamBytesToBeginning() {
			var i = 0;
			for (; i < write_position - read_position; i++) {
				internal_buffer[i] = internal_buffer[i + read_position];
			}

			// Update the length for the new size.
			buffer_stream.SetLength(i);
			//buffer_stream.Position -= write_position;

			// Reset the internal writer and reader positions.
			stream_length = write_position = i;
			read_position = 0;
		}

		/// <summary>
		/// Writes the specified bytes to the FrameBuilder and parses them as they are copied.
		/// </summary>
		/// <param name="buffer">Byte buffer to write and parse.</param>
		/// <param name="offset">Offset in the byte buffer to copy from.</param>
		/// <param name="count">Number of bytes to write into the builder.</param>
		public void Write(byte[] buffer, int offset, int count) {
			while (count > 0) {
				int max_write = count;
				// If we are over the byte limitation, then move the buffer back to the beginning of the stream and reset the stream.
				if (count + write_position > internal_buffer.Length) {
					MoveStreamBytesToBeginning();
					max_write = Math.Min(Math.Abs(count - write_position), count);
				}


				WriteInternalPart(buffer, offset, max_write);
				offset += max_write;
				count -= max_write;
				//count 

			}
		}

		/// <summary>
		/// Writes the specified partial bytes to the FrameBuilder and parses them as they are copied.
		/// </summary>
		/// <param name="buffer">Byte buffer to write and parse.</param>
		/// <param name="offset">Offset in the byte buffer to copy from.</param>
		/// <param name="count">Number of bytes to write into the builder.</param>
		private void WriteInternalPart(byte[] buffer, int offset, int count) {
			// Write the incoming bytes to the stream.
			WriteInternal(buffer, offset, count);

			// Loop until we require more data
			while (true) {
				if (current_frame_type == MqFrameType.Unset) {
					var frame_type_bytes = new byte[1];

					// This will always return one byte.
					ReadInternal(frame_type_bytes, 0, 1);

					if (frame_type_bytes[0] > max_type_enum) {
						throw new InvalidDataException(
							$"FrameBuilder was sent a frame with an invalid type.  Type sent: {frame_type_bytes[0]}");
					}

					current_frame_type = (MqFrameType) frame_type_bytes[0];
				}

				if (current_frame_type == MqFrameType.Empty ||
					current_frame_type == MqFrameType.EmptyLast ||
					current_frame_type == MqFrameType.Ping) {
					EnqueueAndReset();
					break;
				}

				// Read the length from the stream if there are enough buffer.
				if (current_frame_data == null && stream_length >= 2) {
					var frame_len = new byte[2];

					ReadInternal(frame_len, 0, frame_len.Length);
					var current_frame_length = BitConverter.ToUInt16(frame_len, 0);

					if (current_frame_length < 1) {
						throw new InvalidDataException($"FrameBuilder was sent a frame with an invalid size of {current_frame_length}");
					}

					if (current_frame_length > internal_buffer.Length) {
						throw new InvalidDataException($"Frame size is {current_frame_length} while the maximum size for frames is 16KB.");
					}
					current_frame_data = new byte[current_frame_length];

					// Set the stream back to the position it was at to begin with.
					//buffer_stream.Position = original_position;
				}

				// Read the data into the frame holder.
				if (current_frame_data != null && stream_length >= current_frame_data.Length) {
					ReadInternal(current_frame_data, 0, current_frame_data.Length);

					// Create the frame and enqueue it.
					EnqueueAndReset();

					// If we are at the end of the data, complete this loop and wait for more data.
					if (write_position == read_position) {
						break;
					}
				} else {
					break;
				}
			}
		}

		/// <summary>
		/// Completes the current frame and adds it to the frame list.
		/// </summary>
		private void EnqueueAndReset() {
			Frames.Enqueue(new MqFrame(current_frame_data, current_frame_type, config));
			current_frame_type = MqFrameType.Unset;
			current_frame_data = null;
		}

		/// <summary>
		/// Disposes of all resources held by the builder.
		/// </summary>
		public void Dispose() {
			buffer_stream.Dispose();

			// Delete all the Frames.
			var total_frames = Frames.Count;
			for (int i = 0; i < total_frames; i++) {
				var frame = Frames.Dequeue();

				frame.Dispose();
			}
		}
	}
}