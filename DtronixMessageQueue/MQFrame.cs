using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	// ReSharper disable once InconsistentNaming
	public class MQFrame : IDisposable {

		private byte[] data;


		/// <summary>
		/// Information about this frame and how it relates to other frames.
		/// </summary>
		public MQFrameType FrameType { get; internal set; } = MQFrameType.Empty;

		/// <summary>
		/// Total bytes that this frame contains.
		/// </summary>
		public int DataLength { get; } = -1;

		/// <summary>
		/// True if this frame data has been completely read.
		/// </summary>
		public bool FrameComplete => data != null;

		/// <summary>
		/// Bytes this frame contains.
		/// </summary>
		public byte[] Data => data;

		/// <summary>
		/// Total number of bytes that compose this raw frame.
		/// </summary>
		public int FrameLength {
			get {
				// If this frame is empty, then it has a total of one byte.
				if (FrameType == MQFrameType.Empty) {
					return 1;

				}

				return HeaderLength + DataLength;
			}
		}

		private const int HeaderLength = 5;

		public MQFrame(byte[] bytes, MQFrameType type) {
			data = bytes;
			DataLength = bytes.Length;
			FrameType = type;
		}

		/// <summary>
		/// Returns this frame as a raw byte array.
		/// </summary>
		/// <returns>Frame bytes.</returns>
		public byte[] RawFrame() {
			if (data == null) {
				throw new InvalidOperationException("Can not retrieve frame bytes when frame has not been created.");
			}

			// If this is an empty frame, return an empty byte which corresponds to MQFrameType.Empty
			if (FrameType == MQFrameType.Empty) {
				DataLength = 0;
				return new byte[1];
			}

			byte[] bytes = new byte[HeaderLength + DataLength];

			// Type of frame that this is.
			bytes[0] = (byte) FrameType;



			byte[] size_bytes = BitConverter.GetBytes(DataLength);

			// Copy the length.
			Buffer.BlockCopy(size_bytes, 0, bytes, 1, 4);

			// Copy the data
			Buffer.BlockCopy(data, 0, bytes, HeaderLength, DataLength);

			return bytes;
		}



		

		public void Dispose() {
			input_stream?.Dispose();
			data = null;
		}
	}
}
