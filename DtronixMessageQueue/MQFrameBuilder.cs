using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQFrameBuilder {

		private readonly byte[] buffer = new byte[1024 * 32];

		private byte[] current_frame_data;
		private MQFrameType current_frame_type;

		private int read_position;
		private int write_position;
		private int stream_length;

		private readonly MemoryStream buffer_stream;

		public const int HeaderLength = 3;

		public Queue<MQFrame> Frames { get; } = new Queue<MQFrame>();


		public MQFrameBuilder(int current_length, int current_offset) {
			buffer_stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
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
			buffer_stream.Write(client_bytes, offset, count);
			write_position += count;

			// Update the stream length 
			stream_length = write_position - read_position;
		}



		private void MoveStreamBytesToBeginning() {
			for (var i = 0; i < write_position - read_position; i++) {
				buffer[i] = buffer[i + read_position];
			}

			// Update the length for the new size.
			buffer_stream.SetLength(write_position - read_position);
			//buffer_stream.Position -= write_position;

			// Reset the internal writer and reader positions.
			read_position = 0;
			write_position = write_position - read_position;
		}

		public void Write(byte[] client_bytes, int offset, int count) {


			// If we are over 16000 bytes, then move the client_bytes back to the beginning of the stream and reset the stream.
			if (count + write_position > client_bytes.Length / 2) {
				MoveStreamBytesToBeginning();
			}

			// Write the incoming bytes to the stream.
			WriteInternal(client_bytes, offset, count);

			// Loop until we require more data
			while(true) {
				if (current_frame_type == MQFrameType.Unset) {
					var frame_type_bytes = new byte[1];

					// This will always return one byte.
					ReadInternal(frame_type_bytes, 0, 1);
					current_frame_type = (MQFrameType)frame_type_bytes[0];
				}

				// Read the length from the stream if there are enough client_bytes.
				if (stream_length >= 2) {
					var frame_len = new byte[2];

					ReadInternal(frame_len, 0, frame_len.Length);
					var current_frame_length = BitConverter.ToInt16(frame_len, 0);


					if (current_frame_length > 1024*16) {
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
					Frames.Enqueue(new MQFrame(current_frame_data, current_frame_type));
				} else {
					break;
				}
			}
		}

		/*/// <summary>
		/// Reads from the byte queue and tries to return the number of bytes requested.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		private ArraySegment<byte> InternalRead(int count) {
			var bytes = input_bytes.Peek();
			var read_length = Math.Min(count, bytes.Length - current_offset);

			if (count <= 16 && read_length < count && input_bytes.Count > 1) {
				// Remove this one off the queue since we have read it completely.
				input_bytes.Dequeue();
				var len_read = 0;

				while(input_bytes.Count > 1)
				bytes = input_bytes.Dequeue();

				new MemoryStream()

				return new ArraySegment<byte>(bytes, current_offset, read_length);
			}

			var segment = new ArraySegment<byte>(bytes, current_offset, read_length);

			// If our position is at the end of the array, we need to remove these bytes from the queue and reset out offset.
			if (current_offset + read_length == bytes.Length) {
				// Remove these bytes
				input_bytes.Dequeue();

				current_offset = 0;
			} else {
				current_offset += read_length;
			}

			return segment;
		}
		*/

		/*private bool ParseFrame(ArraySegment<byte> client_bytes) {
			IList<byte> buffer_list = client_bytes;
			int position = 0;

			if (current_frame == null) {
				current_frame = new MQFrame {
					FrameType = current_frame_type = (MQFrameType) buffer_list[0]
				};
				position += 1;

			}



			return true;
		}*/
	}
}
