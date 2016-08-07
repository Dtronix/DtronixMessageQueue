using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	// ReSharper disable once InconsistentNaming
	public class MqFrame : IDisposable {

		private byte[] data;

		/// <summary>
		/// Information about this frame and how it relates to other Frames.
		/// </summary>
		public MqFrameType FrameType { get; private set; }

		/// <summary>
		/// Total bytes that this frame contains.
		/// </summary>
		public int DataLength { get; }

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
				if (FrameType == MqFrameType.Empty) {
					return 1;

				}

				return HeaderLength + DataLength;
			}
		}

		private const int HeaderLength = 3;

		public MqFrame(byte[] bytes, MqFrameType type) {
			if (type == MqFrameType.EmptyLast || type == MqFrameType.Empty) {
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
			FrameType = FrameType == MqFrameType.Empty ? MqFrameType.EmptyLast : MqFrameType.Last;
		}

		/// <summary>
		/// Returns this frame as a raw byte array.
		/// </summary>
		/// <returns>Frame bytes.</returns>
		public byte[] RawFrame() {
			if (data == null) {
				throw new InvalidOperationException("Can not retrieve frame bytes when frame has not been created.");
			}

			// If this is an empty frame, return an empty byte which corresponds to MqFrameType.Empty
			if (FrameType == MqFrameType.Empty || FrameType == MqFrameType.Empty) {
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

		public override string ToString() {
			return $"MqFrame totaling {DataLength:N0} bytes; Type: {FrameType}";
		}


		public void Dispose() {
			data = null;
		}
	}
}
