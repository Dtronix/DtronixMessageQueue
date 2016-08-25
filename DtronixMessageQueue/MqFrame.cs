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
		private MqFrameType frame_type;

		/// <summary>
		/// Information about this frame and how it relates to other Frames.
		/// </summary>
		public MqFrameType FrameType {
			get { return frame_type; }
			set {
				if (value == MqFrameType.EmptyLast || value == MqFrameType.Empty) {
					if (buffer != null && buffer.Length != 0) {
						throw new ArgumentException($"Can not set frame to {value} when buffer is not null or empty.");
					}
				}
				frame_type = value; 
				
			}
		}

		public const int MaxFrameSize = 1024*4 - HeaderLength;

		public const int HeaderLength = 3;

		/// <summary>
		/// Total bytes that this frame contains.
		/// </summary>
		public int DataLength => buffer?.Length ?? 0;

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

		public MqFrame(byte[] bytes) : this(bytes, MqFrameType.Unset) {

		}


		public MqFrame(byte[] bytes, MqFrameType type) {
			if (bytes?.Length > MaxFrameSize) {
				throw new ArgumentException("Byte array passed is larger than the maximum frame size allowed.  Must be less than " + MaxFrameSize, nameof(bytes));
			}
			buffer = bytes;
			FrameType = type;
		}


		/// <summary>
		/// Sets this frame to be the last frame of a message.
		/// </summary>
		public void SetLast() {
			if (FrameType != MqFrameType.Command) {
				FrameType = FrameType == MqFrameType.Empty ? MqFrameType.EmptyLast : MqFrameType.Last;
			}
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

			var bytes = new byte[HeaderLength + buffer.Length];

			// Type of frame that this is.
			bytes[0] = (byte)FrameType;

			var size_bytes = BitConverter.GetBytes((short)buffer.Length);

			// Copy the length.
			System.Buffer.BlockCopy(size_bytes, 0, bytes, 1, 2);

			// Copy the byte_buffer
			System.Buffer.BlockCopy(buffer, 0, bytes, HeaderLength, buffer.Length);

			return bytes;
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
		public void Write(int index, bool value) {
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
		public void Write(int index, byte value) {
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
		public void Write(int index, sbyte value) {
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
		/// Writes a char value at the specified index.
		/// 1 Byte.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		public void Write(int index, char value) {
			buffer[index] = (byte)value;
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
		public void Write(int index, short value) {
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
		public void Write(int index, ushort value) {
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
		public void Write(int index, int value) {
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
		public void Write(int index, uint value) {
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
		public void Write(int index, long value) {
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
		public void Write(int index, ulong value) {
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
		public void Write(int index, decimal value) {
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


		/// <summary>
		/// Reads a ASCII encoded string value at the specified index.
		/// (uint16)(string)
		/// >2 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to read the value from.</param>
		/// <param name="string_length">Specified length of the ASCII string.  Default of -1 means the string is prepended with the length which should be read first automatically.</param>
		public string ReadAscii(int index, int string_length = -1) {
			var prepended_length = string_length == -1;
			if (prepended_length) {
				string_length = ReadUInt16(index);
			}

			var str_buffer = new byte[string_length];
			Read(index + (prepended_length ? 2 : 0), str_buffer, 0, string_length);

			return Encoding.ASCII.GetString(str_buffer);
		}


		/// <summary>
		/// Writes a ASCII encoded string.
		/// (uint16?)(string)
		/// >2|1 Bytes.
		/// </summary>
		/// <param name="index">The zero-based index to write the value to.</param>
		/// <param name="value">Value to write to the specified index.</param>
		/// <param name="prepend_size">If set to true, the written string will follow a uint16 of the length of the encoded string.  Otherwise it will be just an encoded string.</param>
		public void WriteAscii(int index, string value, bool prepend_size = true) {
			var string_bytes = Encoding.ASCII.GetBytes(value);

			if (prepend_size) {
				Write(index, (ushort)string_bytes.Length);
			}

			if (index + string_bytes.Length + (prepend_size ? 2 : 0) > MaxFrameSize) {
				throw new InvalidOperationException("Length of ASCII string is longer than the frame will allow.");
			}

			Write(index + (prepend_size ? 2 : 0), string_bytes, 0, string_bytes.Length);
		}



		/// <summary>
		/// Reads the bytes from this frame.
		/// </summary>
		/// <param name="frame_index">Bytes to start reading from.</param>
		/// <param name="byte_buffer">Buffer to copy the frame bytes to.</param>
		/// <param name="offset">Offset in the byte buffer to copy the frame bytes to.</param>
		/// <param name="count">Number of bytes to try to copy.</param>
		/// <returns>Actual number of bytes read.  May be less than the number requested due to being at the end of the frame.</returns>
		public int Read(int frame_index, byte[] byte_buffer, int offset, int count) {
			if (byte_buffer == null) {
				throw new ArgumentNullException(nameof(byte_buffer), "Buffer is null.");
			}
			if (offset < 0) {
				throw new ArgumentOutOfRangeException(nameof(offset), "Need positive offset.");
			}
			if (count < 0) {
				throw new ArgumentOutOfRangeException(nameof(count), "Need positive number");
			}
			if (byte_buffer.Length - offset < count) {
				throw new ArgumentException("Invalid offset length.");
			}

			var max_len = this.buffer.Length - frame_index;
			var req_len = count;

			count = max_len < req_len ? max_len : req_len;

			System.Buffer.BlockCopy(buffer, frame_index, byte_buffer, offset, count);

			return count;
		}

		/// <summary>
		/// Writes a byte buffer to this frame.
		/// </summary>
		/// <param name="frame_index">Start position in this frame to write to.</param>
		/// <param name="byte_buffer">Buffer to write to this frame.</param>
		/// <param name="offset">Offset in the byte buffer.</param>
		/// <param name="count">Number of bytes to copy to this frame.</param>
		public void Write(int frame_index, byte[] byte_buffer, int offset, int count) {
			System.Buffer.BlockCopy(byte_buffer, offset, buffer, frame_index, count);
		}



		/// <summary>
		/// Creates a string representation of this frame.
		/// </summary>
		/// <returns>A string representation of this frame.</returns>
		public override string ToString() {
			return $"MqFrame totaling {buffer.Length:N0} bytes; Type: {FrameType}";
		}


		/// <summary>
		/// Disposes of this object and its resources.
		/// </summary>
		public void Dispose() {
			buffer = null;
		}
	}
}