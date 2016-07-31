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
		/// Information about this frame and how it relates to other Frames.
		/// </summary>
		public MQFrameType FrameType { get; private set; } = MQFrameType.Empty;

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

		private const int HeaderLength = 3;

		public MQFrame(byte[] bytes, MQFrameType type) {
			if (type == MQFrameType.EmptyLast || type == MQFrameType.Empty) {
				DataLength = 0;
			} else {
				if (bytes.Length > 1024 * 16) {
					throw new ArgumentException("Byte array must be less than 16384 bytes", nameof(bytes));
				}

				DataLength = bytes.Length;
				data = bytes;
			}

			FrameType = type;
		}

		public void SetLast() {
			FrameType = FrameType == MQFrameType.Empty ? MQFrameType.EmptyLast : MQFrameType.Last;
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
			if (FrameType == MQFrameType.Empty || FrameType == MQFrameType.Empty) {
				return new[] {(byte) FrameType};
			}

			var bytes = new byte[HeaderLength + DataLength];

			// Type of frame that this is.
			bytes[0] = (byte) FrameType;



			var size_bytes = BitConverter.GetBytes((short)DataLength);

			// Copy the length.
			Buffer.BlockCopy(size_bytes, 0, bytes, 1, 2);

			// Copy the data
			Buffer.BlockCopy(data, 0, bytes, HeaderLength, DataLength);

			return bytes;
		}



		

		public void Dispose() {
			data = null;
		}
	}
}
