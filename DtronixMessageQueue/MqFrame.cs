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
		/// Gets or sets the frame byte at the specified index.
		/// </summary>
		/// <returns>The byte at the specified index.</returns>
		/// <param name="index">The zero-based index of the byte to get or set.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the message.</exception>
		public byte this[int index] {
			get { return data[index]; }
			set { data[index] = value; }
		}

		/// <summary>
		/// Reads a boolean value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public bool ReadBoolean(int index) {
			return data[index] != 0;
		}

		/// <summary>
		/// Reads a byte value at the specified index.
		/// 1 Byte
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public byte ReadByte(int index) {
			return data[index];
		}


		/// <summary>
		/// Reads a sbyte value at the specified index.
		/// 1 Byte
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public sbyte ReadSByte(int index) {
			return (sbyte)data[index];
		}

		/// <summary>
		/// Reads a char value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public char ReadChar(int index) {
			return (char)data[index];
		}

		/// <summary>
		/// Reads a short value at the specified index.
		/// 2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public short ReadInt16(int index) {
			return (short)(data[index] | data[index + 1] << 8);
		}

		/// <summary>
		/// Reads a ushort value at the specified index.
		/// 2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public ushort ReadUInt16(int index) {
			return (ushort)(data[index] | data[index + 1] << 8);
		}



		/// <summary>
		/// Reads a int value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public int ReadInt32(int index) {
			return data[index] | data[index + 1] << 8 | data[index + 2] << 16 | data[index + 3] << 24;
		}

		/// <summary>
		/// Reads a uint value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public uint ReadUInt32(int index) {
			return (uint)(data[index] | data[index + 1] << 8 | data[index + 2] << 16 | data[index + 3] << 24);
		}

		/// <summary>
		/// Reads a long value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public long ReadInt64(int index) {
			var lo = (uint) (data[index] | data[index + 1] << 8 |
							data[index + 2] << 16 | data[index + 3] << 24);
			var hi = (uint) (data[index + 4] | data[index + 5] << 8 |
							data[index + 6] << 16 | data[index + 7] << 24);
			return (long) hi << 32 | lo;
		}

		/// <summary>
		/// Reads a ulong value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public ulong ReadUInt64(int index) {
			var lo = (uint)(data[index] | data[index + 1] << 8 |
							data[index + 2] << 16 | data[index + 3] << 24);
			var hi = (uint)(data[index + 4] | data[index + 5] << 8 |
							data[index + 6] << 16 | data[index + 7] << 24);
			return (ulong)hi << 32 | lo;
		}

		/// <summary>
		/// Reads a float value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public unsafe float ReadSingle(int index) {
			var tmp_buffer = (uint)(data[index] | data[index+1] << 8 | data[index+2] << 16 | data[index+3] << 24);
			return *(float*)&tmp_buffer;
		}

		/// <summary>
		/// Reads a double value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public unsafe double ReadDouble(int index) {
			var lo = (uint)(data[index] | data[index+1] << 8 |
				data[index+2] << 16 | data[index+3] << 24);
			var hi = (uint)(data[index+4] | data[index+5] << 8 |
				data[index+6] << 16 | data[index+7] << 24);

			var tmp_buffer = ((ulong)hi) << 32 | lo;
			return *(double*)&tmp_buffer;
		}

		/// <summary>
		/// Reads a decimal value at the specified index.
		/// 16 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		public decimal ReadDecimal(int index) {
			var ints = new int[4];
			Buffer.BlockCopy(data, index, ints, 0, 16);
			try {
				return new decimal(ints);
			} catch (ArgumentException e) {
				// ReadDecimal cannot leak out ArgumentException
				throw new ArgumentOutOfRangeException(nameof(index), e);
			}
		}



		public virtual int Read(int frame_index, byte[] buffer, int index, int count) {
			if (buffer == null) {
				throw new ArgumentNullException(nameof(buffer), "Buffer is null.");
			}
			if (index < 0) {
				throw new ArgumentOutOfRangeException(nameof(index), "Need positive index.");
			}
			if (count < 0) {
				throw new ArgumentOutOfRangeException(nameof(count), "Need positive number");
			}
			if (buffer.Length - index < count) {
				throw new ArgumentException("Invalid offset length.");
			}

			var max_len = data.Length - frame_index;
			var req_len = count;

			count = max_len < req_len ? max_len : req_len;

			Buffer.BlockCopy(data, frame_index, buffer, index, count);

			return count;
		}

		/// <summary>
		/// Reads a string value at the specified index.
		/// >2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from</param>
		/// <remarks>
		/// A string consists of a ushort and one or more bytes containing the string.
		/// The ushort is used to determine how long the string is.
		/// </remarks>
		public string ReadString(int index) {
			// Length of the string in bytes, not chars
			int string_length = ReadUInt16(index);

			return string_length == 0 ? string.Empty : Encoding.UTF8.GetString(data, index + 2, string_length);
		}

		public int Read7BitEncodedInt(int index) {
			// Read out an Int32 7 bits at a time.  The high bit
			// of the byte when on means to continue reading more bytes.
			var count = 0;
			var shift = 0;
			byte b;
			do {
				// Check for a corrupted stream.  Read a max of 5 bytes.
				// In a future version, add a DataFormatException.
				if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
				{
					throw new FormatException("Could not parse 7 bit encoded int.");
				}

				// ReadByte handles end of stream cases for us.
				b = data[index++];
				count |= (b & 0x7F) << shift;
				shift += 7;
			} while ((b & 0x80) != 0);
			return count;
		}


		//

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