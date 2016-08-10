using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	
	/// <summary>
	/// Data frame containing raw data to be sent over the transport.
	/// </summary>
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
		public int FrameSize {
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
				if (bytes != null && bytes.Length != 0) {
					throw new ArgumentException($"Message frame initialized as {type} but bytes were passed.  Frame must be passed an empty array.");
				}
				DataLength = 0;
			} else {
				DataLength = bytes.Length;
				data = bytes;
			}

			FrameType = type;
		}

		/// <summary>
		/// Sets this frame to be the last frame of a message.
		/// </summary>
		public void SetLast() {
			FrameType = FrameType == MqFrameType.Empty ? MqFrameType.EmptyLast : MqFrameType.Last;
		}

		/// <summary>
		/// Returns this frame as a raw byte array. (Header + data)
		/// </summary>
		/// <returns>Frame bytes.</returns>
		public byte[] RawFrame() {
			// If this is an empty frame, return an empty byte which corresponds to MqFrameType.Empty or MqFrameType.EmptyLast
			if (FrameType == MqFrameType.Empty || FrameType == MqFrameType.EmptyLast) {
				return new[] { (byte)FrameType };
			}

			if (data == null) {
				throw new InvalidOperationException("Can not retrieve frame bytes when frame has not been created.");
			}

			var bytes = new byte[HeaderLength + DataLength];

			// Type of frame that this is.
			bytes[0] = (byte) FrameType;


			var size_bytes = BitConverter.GetBytes((short) DataLength);

			// Copy the length.
			Buffer.BlockCopy(size_bytes, 0, bytes, 1, 2);

			// Copy the data
			Buffer.BlockCopy(data, 0, bytes, HeaderLength, DataLength);

			return bytes;
		}

		/// <summary>
		/// Creates a string representation of this frame.
		/// </summary>
		/// <returns>A string representation of this frame.</returns>
		public override string ToString() {
			return $"MqFrame totaling {DataLength:N0} bytes; Type: {FrameType}";
		}


		/// <summary>
		/// Disposes of this object and its resources.
		/// </summary>
		public void Dispose() {
			data = null;
		}
	}
}