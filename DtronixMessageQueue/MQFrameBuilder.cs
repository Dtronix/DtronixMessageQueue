using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	internal class MqFrameBuilder : IDisposable {
		private readonly MqPostmaster postmaster;

		private readonly byte[] buffer;
		private SemaphoreSlim seaphore;

		private byte[] current_frame_data;
		private MqFrameType current_frame_type;

		private int read_position;
		private int write_position;
		private int stream_length;

		private readonly MemoryStream buffer_stream;

		public const int HeaderLength = 3;

		public Queue<MqFrame> Frames { get; } = new Queue<MqFrame>();

		public MqFrameBuilder(MqPostmaster postmaster) {
			this.postmaster = postmaster;
			buffer = new byte[postmaster.MaxFrameSize];

			// Debug
			//previous_buffer = new byte[buffer_length];

			buffer_stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
			seaphore = new SemaphoreSlim(1, 1);
		}

		private int ReadInternal(byte[] client_bytes, int offset, int count) {
			buffer_stream.Position = read_position;
			var length = buffer_stream.Read(client_bytes, offset, count);
			read_position += length;

			// Update the stream length 
			stream_length = write_position - read_position;
			return length;
		}

		private void WriteInternal(byte[] client_bytes, int offset, int count) {
			buffer_stream.Position = write_position;
			try {
				buffer_stream.Write(client_bytes, offset, count);
			} catch (Exception e) {
				throw new InvalidDataException("FrameBuilder was sent a frame larger than the session allows.", e);
			}

			write_position += count;

			// Update the stream length 
			stream_length = write_position - read_position;
		}


		private void MoveStreamBytesToBeginning() {
			var i = 0;
			for (; i < write_position - read_position; i++) {
				buffer[i] = buffer[i + read_position];
			}

			// Update the length for the new size.
			buffer_stream.SetLength(i);
			//buffer_stream.Position -= write_position;

			// Reset the internal writer and reader positions.
			stream_length = write_position = i;
			read_position = 0;
		}

		public void Write(byte[] client_bytes, int offset, int count) {
			//seaphore.Wait();

			try {
				// If we are over the byte limitation, then move the client_bytes back to the beginning of the stream and reset the stream.
				if (count + write_position > buffer.Length) {
					MoveStreamBytesToBeginning();
				}

				// Write the incoming bytes to the stream.
				WriteInternal(client_bytes, offset, count);

				// Loop until we require more data
				while (true) {
					if (current_frame_type == MqFrameType.Unset) {
						var frame_type_bytes = new byte[1];

						// This will always return one byte.
						ReadInternal(frame_type_bytes, 0, 1);

						if (frame_type_bytes[0] > 4) {
							throw new InvalidDataException(
								$"FrameBuilder was sent a frame with an invalid type.  Type sent: {frame_type_bytes[0]}");
						}

						current_frame_type = (MqFrameType) frame_type_bytes[0];
					}

					// Read the length from the stream if there are enough client_bytes.
					if (current_frame_data == null && stream_length >= 2) {
						var frame_len = new byte[2];

						ReadInternal(frame_len, 0, frame_len.Length);
						var current_frame_length = BitConverter.ToInt16(frame_len, 0);

						if (current_frame_length < 1) {
							throw new InvalidDataException($"FrameBuilder was sent a frame with an invalid size of {current_frame_length}");
						}

						if (current_frame_length > buffer.Length) {
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
						Frames.Enqueue(new MqFrame(current_frame_data, current_frame_type));

						current_frame_type = MqFrameType.Unset;
						current_frame_data = null;

						// If we are at the end of the data, complete this loop and wait for more data.
						if (write_position == read_position) {
							break;
						}
					} else {
						break;
					}
				}
			} finally {
				//seaphore.Release();
			}
		}

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