using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQFrameBuilder {

		private MemoryStream input_stream;
		private MQFrame curren_frame;
		private MQFrameType current_frame_type;
		private int current_frame_length = -1;

		public Queue<MQFrame> Frames { get; } = new Queue<MQFrame>();

		public MQFrameBuilder() {
			input_stream = new MemoryStream();
		}
		public void Write(byte[] buffer, int offset, int count) {
			input_stream.Write(buffer, offset, count);
			int over_read = 0;
			input_stream.GetBuffer()
			// Loop untill we require more data
			while (over_read > 0) {
				if (current_frame_type == MQFrameType.Unset) {
					current_frame_type = (MQFrameType)buffer[offset];
				}

				// Read the length from the stream if there are enough buffer.
				if (input_stream.Length >= HeaderLength) {
					var frame_len = new byte[4];
					var original_position = input_stream.Position;

					// Set the position of the stream to the location of the length.
					input_stream.Position = 1;
					input_stream.Read(frame_len, 0, frame_len.Length);
					data_length = BitConverter.ToInt32(frame_len, 0);


					if (data_length > 1024 * 16) {
						throw new InvalidDataException($"Frame size is {data_length} while the maximum size for frames is 30KB.");
					}
					data = new byte[data_length];

					// Set the stream back to the position it was at to begin with.
					input_stream.Position = original_position;
				}

				// We have over-read into another frame  Setup a new frame.
				if (DataLength != -1 && input_stream.Length - HeaderLength > DataLength) {
					over_read = (int)input_stream.Length - HeaderLength - DataLength;
					input_stream.SetLength(input_stream.Length - over_read);
				}

				if (DataLength != -1 && input_stream.Length - HeaderLength == DataLength) {
					var input_stream_bytes = input_stream.GetBuffer();
					Buffer.BlockCopy(input_stream_bytes, HeaderLength, data, 0, data_length);
					input_stream.Close();
					input_stream = null;
				}
			}
			

			return over_read;


		}
	}
}
