using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	
	/// <summary>
	/// byte_buffer frame containing raw byte_buffer to be sent over the transport.
	/// </summary>
	public class MqFrame : IDisposable {
		private byte[] buffer;

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
		public byte[] Buffer => buffer;

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
				buffer = bytes;
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
			get { return buffer[index]; }
			set { buffer[index] = value; }
		}

		/// <summary>
		/// Reads a boolean value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public bool ReadBoolean(int index) {
			return buffer[index] != 0;
		}

		/// <summary>
		/// Writes a boolean value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, bool value) {
			buffer[index] = (byte)(value ? 1 : 0);
		}

		/// <summary>
		/// Reads a byte value at the specified index.
		/// 1 Byte
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public byte ReadByte(int index) {
			return buffer[index];
		}

		/// <summary>
		/// Writes a byte value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, byte value) {
			buffer[index] = value;
		}


		/// <summary>
		/// Reads a sbyte value at the specified index.
		/// 1 Byte
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public sbyte ReadSByte(int index) {
			return (sbyte)buffer[index];
		}

		/// <summary>
		/// Writes a sbyte value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, sbyte value) {
			buffer[index] = (byte)value;
		}

		/// <summary>
		/// Reads a char value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public char ReadChar(int index) {
			return (char)buffer[index];
		}

		/// <summary>
		/// Reads a short value at the specified index.
		/// 2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public short ReadInt16(int index) {
			return (short)(buffer[index] | buffer[index + 1] << 8);
		}


		/// <summary>
		/// Writes a short value at the specified index.
		/// 2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, short value) {
			buffer[index + 0] = (byte) value;
			buffer[index + 1] = (byte) (value >> 8);
		}

		/// <summary>
		/// Reads a ushort value at the specified index.
		/// 2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public ushort ReadUInt16(int index) {
			return (ushort)(buffer[index] | buffer[index + 1] << 8);
		}


		/// <summary>
		/// Writes a short value at the specified index.
		/// 2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, ushort value) {
			buffer[index + 0] = (byte)value;
			buffer[index + 1] = (byte)(value >> 8);
		}



		/// <summary>
		/// Reads a int value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public int ReadInt32(int index) {
			return buffer[index] | buffer[index + 1] << 8 | buffer[index + 2] << 16 | buffer[index + 3] << 24;
		}



		/// <summary>
		/// Writes a int value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, int value) {
			buffer[index + 0] = (byte) value;
			buffer[index + 1] = (byte) (value >> 8);
			buffer[index + 2] = (byte) (value >> 16);
			buffer[index + 3] = (byte) (value >> 24);
		}

		/// <summary>
		/// Reads a uint value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public uint ReadUInt32(int index) {
			return (uint)(buffer[index] | buffer[index + 1] << 8 | buffer[index + 2] << 16 | buffer[index + 3] << 24);
		}

		/// <summary>
		/// Writes a uint value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, uint value) {
			buffer[index + 0] = (byte)value;
			buffer[index + 1] = (byte)(value >> 8);
			buffer[index + 2] = (byte)(value >> 16);
			buffer[index + 3] = (byte)(value >> 24);
		}

		/// <summary>
		/// Reads a long value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public long ReadInt64(int index) {
			var lo = (uint) (buffer[index] | buffer[index + 1] << 8 |
							buffer[index + 2] << 16 | buffer[index + 3] << 24);
			var hi = (uint) (buffer[index + 4] | buffer[index + 5] << 8 |
							buffer[index + 6] << 16 | buffer[index + 7] << 24);
			return (long) hi << 32 | lo;
		}

		/// <summary>
		/// Writes a long value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, long value) {
			buffer[index + 0] = (byte) value;
			buffer[index + 1] = (byte) (value >> 8);
			buffer[index + 2] = (byte) (value >> 16);
			buffer[index + 3] = (byte) (value >> 24);
			buffer[index + 4] = (byte) (value >> 32);
			buffer[index + 5] = (byte) (value >> 40);
			buffer[index + 6] = (byte) (value >> 48);
			buffer[index + 7] = (byte) (value >> 56);
		}

		/// <summary>
		/// Reads a ulong value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public ulong ReadUInt64(int index) {
			var lo = (uint)(buffer[index] | buffer[index + 1] << 8 |
							buffer[index + 2] << 16 | buffer[index + 3] << 24);
			var hi = (uint)(buffer[index + 4] | buffer[index + 5] << 8 |
							buffer[index + 6] << 16 | buffer[index + 7] << 24);
			return (ulong)hi << 32 | lo;
		}

		/// <summary>
		/// Writes a ulong value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, ulong value) {
			buffer[index + 0] = (byte)value;
			buffer[index + 1] = (byte)(value >> 8);
			buffer[index + 2] = (byte)(value >> 16);
			buffer[index + 3] = (byte)(value >> 24);
			buffer[index + 4] = (byte)(value >> 32);
			buffer[index + 5] = (byte)(value >> 40);
			buffer[index + 6] = (byte)(value >> 48);
			buffer[index + 7] = (byte)(value >> 56);
		}

		/// <summary>
		/// Reads a float value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public unsafe float ReadSingle(int index) {
			var tmp_buffer = (uint)(buffer[index] | buffer[index+1] << 8 | buffer[index+2] << 16 | buffer[index+3] << 24);
			return *(float*)&tmp_buffer;
		}






		/// <summary>
		/// Writes a float value at the specified index.
		/// 4 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public unsafe void Write(int index, float value) {
			var tmp_value = *(uint*) &value;
			buffer[index + 0] = (byte) tmp_value;
			buffer[index + 1] = (byte) (tmp_value >> 8);
			buffer[index + 2] = (byte) (tmp_value >> 16);
			buffer[index + 3] = (byte) (tmp_value >> 24);
		}


		/// <summary>
		/// Reads a double value at the specified index.
		/// 8 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public unsafe double ReadDouble(int index) {
			var lo = (uint)(buffer[index] | buffer[index+1] << 8 |
				buffer[index+2] << 16 | buffer[index+3] << 24);
			var hi = (uint)(buffer[index+4] | buffer[index+5] << 8 |
				buffer[index+6] << 16 | buffer[index+7] << 24);

			var tmp_buffer = (ulong)hi << 32 | lo;
			return *(double*)&tmp_buffer;
		}


		/// <summary>
		/// Writes a double value at the specified index.
		/// 8 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public unsafe void Write(int index, double value) {
			var tmp_value = *(ulong*) &value;
			buffer[index + 0] = (byte) tmp_value;
			buffer[index + 1] = (byte) (tmp_value >> 8);
			buffer[index + 2] = (byte) (tmp_value >> 16);
			buffer[index + 3] = (byte) (tmp_value >> 24);
			buffer[index + 4] = (byte) (tmp_value >> 32);
			buffer[index + 5] = (byte) (tmp_value >> 40);
			buffer[index + 6] = (byte) (tmp_value >> 48);
			buffer[index + 7] = (byte) (tmp_value >> 56);
		}

		/// <summary>
		/// Reads a decimal value at the specified index.
		/// 16 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		public decimal ReadDecimal(int index) {
			var ints = new int[4];
			System.Buffer.BlockCopy(buffer, index, ints, 0, 16);
			try {
				return new decimal(ints);
			} catch (ArgumentException e) {
				// ReadDecimal cannot leak out ArgumentException
				throw new ArgumentOutOfRangeException(nameof(index), e);
			}
		}


		/// <summary>
		/// Writes a decimal value at the specified index.
		/// 16 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public virtual void Write(int index, decimal value) {
			var bits = decimal.GetBits(value);

			var lo = bits[0];
			buffer[index+0] = (byte)lo;
			buffer[index+1] = (byte)(lo >> 8);
			buffer[index+2] = (byte)(lo >> 16);
			buffer[index+3] = (byte)(lo >> 24);

			var mid = bits[1];
			buffer[index+4] = (byte)mid;
			buffer[index+5] = (byte)(mid >> 8);
			buffer[index+6] = (byte)(mid >> 16);
			buffer[index+7] = (byte)(mid >> 24);

			var hi = bits[2];
			buffer[index+8] = (byte)hi;
			buffer[index+9] = (byte)(hi >> 8);
			buffer[index+10] = (byte)(hi >> 16);
			buffer[index+11] = (byte)(hi >> 24);

			var flags = bits[3];
			buffer[index+12] = (byte)flags;
			buffer[index+13] = (byte)(flags >> 8);
			buffer[index+14] = (byte)(flags >> 16);
			buffer[index+15] = (byte)(flags >> 24);
		}



		public virtual int Read(int frame_index, byte[] byte_buffer, int index, int count) {
			if (byte_buffer == null) {
				throw new ArgumentNullException(nameof(byte_buffer), "Buffer is null.");
			}
			if (index < 0) {
				throw new ArgumentOutOfRangeException(nameof(index), "Need positive index.");
			}
			if (count < 0) {
				throw new ArgumentOutOfRangeException(nameof(count), "Need positive number");
			}
			if (byte_buffer.Length - index < count) {
				throw new ArgumentException("Invalid offset length.");
			}

			var max_len = this.buffer.Length - frame_index;
			var req_len = count;

			count = max_len < req_len ? max_len : req_len;

			System.Buffer.BlockCopy(buffer, frame_index, byte_buffer, index, count);

			return count;
		}

		public virtual void Write(int frame_index, byte[] byte_buffer, int index, int count) {
			System.Buffer.BlockCopy(byte_buffer, index, buffer, frame_index, count);
		}

		/// <summary>
		/// Reads a string value at the specified index.
		/// >2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		/// <remarks>
		/// A string consists of a ushort and one or more bytes containing the string.
		/// The ushort is used to determine how long the string is.
		/// </remarks>
		public string ReadString(int index) {
			// Length of the string in bytes, not chars
			int string_length = ReadUInt16(index);

			return string_length == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, index + 2, string_length);
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
				b = buffer[index++];
				count |= (b & 0x7F) << shift;
				shift += 7;
			} while ((b & 0x80) != 0);
			return count;
		}



		/// <summary>
		/// Sets this frame to be the last frame of a message.
		/// </summary>
		public void SetLast() {
			FrameType = FrameType == MqFrameType.Empty ? MqFrameType.EmptyLast : MqFrameType.Last;
		}

		/// <summary>
		/// Returns this frame as a raw byte array. (Header + byte_buffer)
		/// </summary>
		/// <returns>Frame bytes.</returns>
		public byte[] RawFrame() {
			// If this is an empty frame, return an empty byte which corresponds to MqFrameType.Empty or MqFrameType.EmptyLast
			if (FrameType == MqFrameType.Empty || FrameType == MqFrameType.EmptyLast) {
				return new[] { (byte)FrameType };
			}

			if (buffer == null) {
				throw new InvalidOperationException("Can not retrieve frame bytes when frame has not been created.");
			}

			var bytes = new byte[HeaderLength + DataLength];

			// Type of frame that this is.
			bytes[0] = (byte) FrameType;


			var size_bytes = BitConverter.GetBytes((short) DataLength);

			// Copy the length.
			System.Buffer.BlockCopy(size_bytes, 0, bytes, 1, 2);

			// Copy the byte_buffer
			System.Buffer.BlockCopy(buffer, 0, bytes, HeaderLength, DataLength);

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
			buffer = null;
		}
	}
}