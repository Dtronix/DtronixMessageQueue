using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MqMessageReader {
		private int position;
		private MqMessage message;

		private MqFrame current_frame;

		private int message_position;

		public MqMessage Message {
			get { return message; }
			set {
				message = value;
				current_frame = value?[0];
				position = 0;
				message_position = 0;
			}
		}

		public bool IsAtEnd {
			get {
				var last_frame = message.Frames[message.Frames.Count - 1];
				return current_frame == last_frame && last_frame.DataLength == position;

			}
		}

		public MqMessageReader() : this(null){
		}

		public MqMessageReader(MqMessage initial_message) {
			Message = initial_message;
		}


		private void EnsureBuffer(int length) {
			
			if (position + length > current_frame.DataLength) {
				throw new InvalidOperationException("Trying to read simple type across frames which is not allowed.");
			}

			if (position + length > current_frame.DataLength) {
				NextNonEmptyFrame();
			}
		}

		private void NextNonEmptyFrame() {
			position = 0;
			// Increment until we reach the next non-empty frame.
			do {
				message_position++;
			} while (message[message_position]?.FrameType == MqFrameType.Empty);

			current_frame = message_position >= message.Count ? null : message[message_position];

		}

		/// <summary>
		/// Reads a boolean value.
		/// 1 Byte.
		/// </summary>
		public bool ReadBoolean() {
			EnsureBuffer(1);
			var value = current_frame.ReadBoolean(position);
			position += 1;
			return value;
		}


		/// <summary>
		/// Reads a byte value.
		/// 1 Byte
		/// </summary>
		public byte ReadByte() {
			EnsureBuffer(1);
			var value = current_frame.ReadByte(position);
			position += 1;
			return value;
		}

		/// <summary>
		/// Reads a sbyte value.
		/// 1 Byte
		/// </summary>
		public sbyte ReadSByte() {
			EnsureBuffer(1);
			var value = current_frame.ReadSByte(position);
			position += 1;
			return value;
		}

		/// <summary>
		/// Reads a char value.
		/// 1 Byte.
		/// </summary>
		public char ReadChar() {
			EnsureBuffer(1);
			var value = current_frame.ReadChar(position);
			position += 1;
			return value;
		}

		/// <summary>
		/// Reads a short value.
		/// 2 Bytes.
		/// </summary>
		public short ReadInt16() {
			EnsureBuffer(2);
			var value = current_frame.ReadInt16(position);
			position += 2;
			return value;
		}

		/// <summary>
		/// Reads a ushort value.
		/// 2 Bytes.
		/// </summary>
		public ushort ReadUInt16() {
			EnsureBuffer(2);
			var value = current_frame.ReadUInt16(position);
			position += 2;
			return value;
		}


		/// <summary>
		/// Reads a int value.
		/// 4 Bytes.
		/// </summary>
		public int ReadInt32() {
			EnsureBuffer(4);
			var value = current_frame.ReadInt32(position);
			position += 4;
			return value;
		}


		/// <summary>
		/// Reads a uint value.
		/// 4 Bytes.
		/// </summary>
		public uint ReadUInt32() {
			EnsureBuffer(4);
			var value = current_frame.ReadUInt32(position);
			position += 4;
			return value;
		}

		/// <summary>
		/// Reads a long value.
		/// 8 Bytes.
		/// </summary>
		public long ReadInt64() {
			EnsureBuffer(8);
			var value = current_frame.ReadInt64(position);
			position += 8;
			return value;
		}


		/// <summary>
		/// Reads a ulong value.
		/// 8 Bytes.
		/// </summary>
		public ulong ReadUInt64() {
			EnsureBuffer(8);
			var value = current_frame.ReadUInt64(position);
			position += 8;
			return value;
		}


		/// <summary>
		/// Reads a float value.
		/// 4 Bytes.
		/// </summary>
		public float ReadSingle() {
			EnsureBuffer(4);
			var value = current_frame.ReadSingle(position);
			position += 4;
			return value;
		}


		/// <summary>
		/// Reads a double value.
		/// 8 Bytes.
		/// </summary>
		public double ReadDouble() {
			EnsureBuffer(8);
			var value = current_frame.ReadDouble(position);
			position += 8;
			return value;
		}

		/// <summary>
		/// Reads a decimal value.
		/// 16 Bytes.
		/// </summary>
		public decimal ReadDecimal() {
			EnsureBuffer(16);
			var value = current_frame.ReadDecimal(position);
			position += 16;
			return value;
		}

		/// <summary>
		/// Writes a string.
		/// >4 Bytes.
		/// 1 or more frames.
		/// </summary>
		public string ReadString() {
			// Write the length prefix

			var str_len = ReadInt32();
			var str_buffer = new byte[str_len];
			Read(str_buffer, 0, str_len);

			return Encoding.UTF8.GetString(str_buffer);

		}

		/// <summary>
		/// Reads the bytes from this message.
		/// </summary>
		/// <param name="byte_buffer">Buffer to copy the frame bytes to.</param>
		/// <param name="offset">Offset in the byte buffer to copy the frame bytes to.</param>
		/// <param name="count">Number of bytes to try to copy.</param>
		/// <returns>Actual number of bytes read.  May be less than the number requested due to being at the end of the frame.</returns>
		public int Read(byte[] byte_buffer, int offset, int count) {
			var total_read = 0;
			while (offset < count) {
				var max_read_length = current_frame.DataLength - position;
				var read_length = count - total_read < max_read_length ? count - total_read : max_read_length;
				// If we are at the end of this max frame size, get a new one.
				if (max_read_length == 0) {
					NextNonEmptyFrame();
					continue;
				}

				var read = current_frame.Read(position, byte_buffer, offset, read_length);
				position += read;
				total_read += read;
				offset += read;
			}

			return total_read;
		}


	}
}
