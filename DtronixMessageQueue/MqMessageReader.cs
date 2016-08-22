using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MqMessageReader : BinaryReader {
		private readonly Encoding encoding;
		private int position;
		private MqMessage message;

		private MqFrame current_frame;

		private int message_position;

		private readonly bool two_bytes_per_char;

		private const int MaxCharBytesSize = 128;

		private byte[] char_bytes;


		private readonly Decoder decoder;

		/// <summary>
		/// Current frame that is being read.
		/// </summary>
		public MqFrame CurrentFrame => current_frame;


		/// <summary>
		/// Gets the current message.
		/// Setting a message resets reading positions to the beginning.
		/// </summary>
		public MqMessage Message {
			get { return message; }
			set {
				message = value;
				current_frame = value?[0];
				position = 0;
				message_position = 0;
			}
		}

		/// <summary>
		/// Unused. Stream.Null
		/// </summary>
		public override Stream BaseStream { get; } = Stream.Null;

		/// <summary>
		/// True if we are at the end of the last frame of the message.
		/// </summary>
		public bool IsAtEnd {
			get {
				var last_frame = message.Frames[message.Frames.Count - 1];
				return current_frame == last_frame && last_frame.DataLength == position;
			}
		}


		/// <summary>
		/// Creates a new message reader with no message and the default encoding of UTF8.
		/// </summary>
		public MqMessageReader() : this(null){
		}

		/// <summary>
		/// Creates a new message reader with the specified message to read and the default encoding of UTF8.
		/// </summary>
		/// <param name="initial_message">Message to read.</param>
		public MqMessageReader(MqMessage initial_message) : this(initial_message, Encoding.UTF8) {
		}

		/// <summary>
		/// Creates a new message reader with the specified message to read and the specified encoding.
		/// </summary>
		/// <param name="initial_message">Message to read.</param>
		/// <param name="encoding">Encoding to use for string interpretation.</param>
		public MqMessageReader(MqMessage initial_message, Encoding encoding) : base(Stream.Null) {
			this.encoding = encoding;
			decoder = this.encoding.GetDecoder();
			two_bytes_per_char = encoding is UnicodeEncoding;
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
		/// Advances the reader to the next frame and resets reading positions.
		/// </summary>
		public void NextFrame() {
			position = 0;
			message_position++;
			current_frame = message[message_position];
		}

		/// <summary>
		/// Reads a boolean value.
		/// 1 Byte.
		/// </summary>
		public override bool ReadBoolean() {
			EnsureBuffer(1);
			var value = current_frame.ReadBoolean(position);
			position += 1;
			return value;
		}


		/// <summary>
		/// Reads a byte value.
		/// 1 Byte
		/// </summary>
		public override byte ReadByte() {
			EnsureBuffer(1);
			var value = current_frame.ReadByte(position);
			position += 1;
			return value;
		}

		/// <summary>
		/// Reads a sbyte value.
		/// 1 Byte
		/// </summary>
		public override sbyte ReadSByte() {
			EnsureBuffer(1);
			var value = current_frame.ReadSByte(position);
			position += 1;
			return value;
		}


		/// <summary>
		/// Reads a char value.
		/// >=1 Byte.
		/// </summary>
		public override char ReadChar() {
			var read_char = new char[1];
			Read(read_char, 0, 1);
			return read_char[0];
		}


		/// <summary>
		/// Reads the specified number of chars from the message.
		/// >1 Byte.
		/// 1 or more frames.
		/// </summary>
		/// <param name="count">Number of chars to read.</param>
		/// <returns>Char array.</returns>
		public override char[] ReadChars(int count) {
			var chars = new char[count];
			Read(chars, 0, count);
			return chars;
		}


		/// <summary>
		/// Reads the specified number of chars from the message into the passed char buffer.
		/// </summary>
		/// <param name="buffer">Buffer of chars to copy into.</param>
		/// <param name="index">Starting index start copying the chars into.</param>
		/// <param name="count">Number of chars to read.</param>
		/// <returns>Number of chars read from the message. Can be less than requested if the end of the message is reached.</returns>
		public override int Read(char[] buffer, int index, int count) {
			var chars_remaining = count;

			if (char_bytes == null) {
				char_bytes = new byte[MaxCharBytesSize];
			}

			while (chars_remaining > 0) {
				var chars_read = 0;
				// We really want to know what the minimum number of bytes per char
				// is for our encoding.  Otherwise for UnicodeEncoding we'd have to
				// do ~1+log(n) reads to read n characters. 
				var num_bytes = chars_remaining;


				// TODO: special case for UTF8Decoder when there are residual bytes from previous loop 
				/*UTF8Encoding.UTF8Decoder decoder = m_decoder as UTF8Encoding.UTF8Decoder;
				if (decoder != null && decoder.HasState && numBytes > 1) {
					numBytes -= 1;
				}*/


				if (two_bytes_per_char) {
					num_bytes <<= 1;
				}

				if (num_bytes > MaxCharBytesSize) {
					num_bytes = MaxCharBytesSize;
				}

				var char_position = 0;

				num_bytes = Read(char_bytes, 0, num_bytes);
				var byte_buffer = char_bytes;


				if (num_bytes == 0) {
					return count - chars_remaining;
				}

				unsafe
				{
					fixed (byte* bytes = byte_buffer)
					fixed (char* chars = buffer) {
						chars_read = decoder.GetChars(bytes + char_position, num_bytes, chars + index, chars_remaining, false);
					}
				}

				chars_remaining -= chars_read;
				index += chars_read;
			}

			// we may have read fewer than the number of characters requested if end of stream reached 
			// or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
			return count - chars_remaining;
		}

		/// <summary>
		/// Peeks at the next char value.
		/// >=1 Byte.
		/// </summary>
		public override int PeekChar() {
			// Store the temporary state of the reader.
			var previous_frame = current_frame;
			var previous_position = position;
			var previous_message_position = message_position;

			var value = ReadChar();

			// Restore the original state of the reader.
			current_frame = previous_frame;
			position = previous_position;
			message_position = previous_message_position;

			return (int) value;
		}

		/// <summary>
		/// Reads a short value.
		/// 2 Bytes.
		/// </summary>
		public override short ReadInt16() {
			EnsureBuffer(2);
			var value = current_frame.ReadInt16(position);
			position += 2;
			return value;
		}

		/// <summary>
		/// Reads a ushort value.
		/// 2 Bytes.
		/// </summary>
		public override ushort ReadUInt16() {
			EnsureBuffer(2);
			var value = current_frame.ReadUInt16(position);
			position += 2;
			return value;
		}


		/// <summary>
		/// Reads a int value.
		/// 4 Bytes.
		/// </summary>
		public override int Read() {
			return ReadInt32();
		}


		/// <summary>
		/// Reads a int value.
		/// 4 Bytes.
		/// </summary>
		public override int ReadInt32() {
			EnsureBuffer(4);
			var value = current_frame.ReadInt32(position);
			position += 4;
			return value;
		}


		/// <summary>
		/// Reads a uint value.
		/// 4 Bytes.
		/// </summary>
		public override uint ReadUInt32() {
			EnsureBuffer(4);
			var value = current_frame.ReadUInt32(position);
			position += 4;
			return value;
		}

		/// <summary>
		/// Reads a long value.
		/// 8 Bytes.
		/// </summary>
		public override long ReadInt64() {
			EnsureBuffer(8);
			var value = current_frame.ReadInt64(position);
			position += 8;
			return value;
		}


		/// <summary>
		/// Reads a ulong value.
		/// 8 Bytes.
		/// </summary>
		public override ulong ReadUInt64() {
			EnsureBuffer(8);
			var value = current_frame.ReadUInt64(position);
			position += 8;
			return value;
		}


		/// <summary>
		/// Reads a float value.
		/// 4 Bytes.
		/// </summary>
		public override float ReadSingle() {
			EnsureBuffer(4);
			var value = current_frame.ReadSingle(position);
			position += 4;
			return value;
		}


		/// <summary>
		/// Reads a double value.
		/// 8 Bytes.
		/// </summary>
		public override double ReadDouble() {
			EnsureBuffer(8);
			var value = current_frame.ReadDouble(position);
			position += 8;
			return value;
		}

		/// <summary>
		/// Reads a decimal value.
		/// 16 Bytes.
		/// </summary>
		public override decimal ReadDecimal() {
			EnsureBuffer(16);
			var value = current_frame.ReadDecimal(position);
			position += 16;
			return value;
		}

		/// <summary>
		/// Reads a string.
		/// >4 Bytes.
		/// 1 or more frames.
		/// </summary>
		public override string ReadString() {
			// Write the length prefix

			var str_len = ReadInt32();
			var str_buffer = new byte[str_len];
			Read(str_buffer, 0, str_len);

			return encoding.GetString(str_buffer);

		}

		/// <summary>
		/// Reads the specified number of bytes from the message.
		/// >1 Byte.
		/// 1 or more frames.
		/// </summary>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>Filled byte array with the data from the message.</returns>
		public override byte[] ReadBytes(int count) {
			var bytes = new byte[count];
			Read(bytes, 0, count);
			return bytes;
		}

		/// <summary>
		/// Reads the bytes from this message.
		/// </summary>
		/// <param name="byte_buffer">Buffer to copy the frame bytes to.</param>
		/// <param name="offset">Offset in the byte buffer to copy the frame bytes to.</param>
		/// <param name="count">Number of bytes to try to copy.</param>
		/// <returns>Actual number of bytes read.  May be less than the number requested due to being at the end of the frame.</returns>
		public override int Read(byte[] byte_buffer, int offset, int count) {
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
