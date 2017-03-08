using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DtronixMessageQueue {

	/// <summary>
	/// Reader to parse a message into sub-components.
	/// </summary>
	public class MqMessageReader : BinaryReader {

		/// <summary>
		/// Current encoding to use for char and string parsing.
		/// </summary>
		private readonly Encoding encoding;

		/// <summary>
		/// Current decoder to use for char and string parsing.
		/// </summary>
		private readonly Decoder decoder;

		/// <summary>
		/// Current position in the frame that is being read.
		/// </summary>
		private int frame_position;

		/// <summary>
		/// Position in the entire message read.
		/// </summary>
		private int absolute_position;

		/// <summary>
		/// Current message being read.
		/// </summary>
		private MqMessage message;

		/// <summary>
		/// Current frame being read.
		/// </summary>
		private MqFrame current_frame;

		/// <summary>
		/// Current frame in the message that is being read.
		/// </summary>
		private int message_position;

		/// <summary>
		/// True if this encoding always has two bytes per character.
		/// </summary>
		private readonly bool two_bytes_per_char;

		/// <summary>
		/// Maximum number of chars to read from the message.
		/// </summary>
		private const int MaxCharBytesSize = 128;

		/// <summary>
		/// Number of bytes each char takes for the specified encoding.
		/// </summary>
		private byte[] char_bytes;

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
				frame_position = 0;
				message_position = 0;
				absolute_position = 0;
			}
		}

		/// <summary>
		/// Get or set the byte position in the message.
		/// </summary>
		public int Position {
			get {
				return absolute_position;
			}
			set {
				current_frame = message?[0];
				frame_position = 0;
				message_position = 0;
				absolute_position = 0;
				if (value != 0) {
					Skip(value);
				}
			}
		} 

		/// <summary>
		/// Total length of the bytes in this message.
		/// </summary>
		public int Length => message.Sum(frm => frm.DataLength);

		/// <summary>
		/// Unused. Stream.Null
		/// </summary>
		public override Stream BaseStream { get; } = Stream.Null;

		/// <summary>
		/// True if we are at the end of the last frame of the message.
		/// </summary>
		public bool IsAtEnd {
			get {
				var last_frame = message[message.Count - 1];
				return current_frame == last_frame && last_frame.DataLength == frame_position;
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

		/// <summary>
		/// Ensures the specified number of bytes is available to read from.
		/// </summary>
		/// <param name="length">Number of bytes to ensure available.</param>
		private void EnsureBuffer(int length) {
			if (frame_position + length > current_frame.DataLength) {
				throw new InvalidOperationException("Trying to read simple type across frames which is not allowed.");
			}
		}

		/// <summary>
		/// Skips over to the next non-empty frame in the message.
		/// </summary>
		private void NextNonEmptyFrame() {
			frame_position = 0;
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
			frame_position = 0;
			message_position += 1;
			current_frame = message[message_position];
		}

		/// <summary>
		/// Reads a boolean value.
		/// 1 Byte.
		/// </summary>
		public override bool ReadBoolean() {
			EnsureBuffer(1);
			var value = current_frame.ReadBoolean(frame_position);
			frame_position += 1;
			absolute_position += 1;
			return value;
		}


		/// <summary>
		/// Reads a byte value.
		/// 1 Byte
		/// </summary>
		public override byte ReadByte() {
			EnsureBuffer(1);
			var value = current_frame.ReadByte(frame_position);
			frame_position += 1;
			absolute_position += 1;
			return value;
		}

		/// <summary>
		/// Reads a sbyte value.
		/// 1 Byte
		/// </summary>
		public override sbyte ReadSByte() {
			EnsureBuffer(1);
			var value = current_frame.ReadSByte(frame_position);
			frame_position += 1;
			absolute_position += 1;
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
			var previous_position = frame_position;
			var previous_message_position = message_position;

			var value = ReadChar();

			// Restore the original state of the reader.
			current_frame = previous_frame;
			frame_position = previous_position;
			message_position = previous_message_position;

			return (int) value;
		}

		/// <summary>
		/// Reads a short value.
		/// 2 Bytes.
		/// </summary>
		public override short ReadInt16() {
			EnsureBuffer(2);
			var value = current_frame.ReadInt16(frame_position);
			frame_position += 2;
			absolute_position += 2;
			return value;
		}

		/// <summary>
		/// Reads a ushort value.
		/// 2 Bytes.
		/// </summary>
		public override ushort ReadUInt16() {
			EnsureBuffer(2);
			var value = current_frame.ReadUInt16(frame_position);
			frame_position += 2;
			absolute_position += 2;
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
			var value = current_frame.ReadInt32(frame_position);
			frame_position += 4;
			absolute_position += 4;
			return value;
		}


		/// <summary>
		/// Reads a uint value.
		/// 4 Bytes.
		/// </summary>
		public override uint ReadUInt32() {
			EnsureBuffer(4);
			var value = current_frame.ReadUInt32(frame_position);
			frame_position += 4;
			absolute_position += 4;
			return value;
		}

		/// <summary>
		/// Reads a long value.
		/// 8 Bytes.
		/// </summary>
		public override long ReadInt64() {
			EnsureBuffer(8);
			var value = current_frame.ReadInt64(frame_position);
			frame_position += 8;
			absolute_position += 8;
			return value;
		}


		/// <summary>
		/// Reads a ulong value.
		/// 8 Bytes.
		/// </summary>
		public override ulong ReadUInt64() {
			EnsureBuffer(8);
			var value = current_frame.ReadUInt64(frame_position);
			frame_position += 8;
			absolute_position += 8;
			return value;
		}


		/// <summary>
		/// Reads a float value.
		/// 4 Bytes.
		/// </summary>
		public override float ReadSingle() {
			EnsureBuffer(4);
			var value = current_frame.ReadSingle(frame_position);
			frame_position += 4;
			absolute_position += 4;
			return value;
		}


		/// <summary>
		/// Reads a double value.
		/// 8 Bytes.
		/// </summary>
		public override double ReadDouble() {
			EnsureBuffer(8);
			var value = current_frame.ReadDouble(frame_position);
			frame_position += 8;
			absolute_position += 8;
			return value;
		}

		/// <summary>
		/// Reads a decimal value.
		/// 16 Bytes.
		/// </summary>
		public override decimal ReadDecimal() {
			EnsureBuffer(16);
			var value = current_frame.ReadDecimal(frame_position);
			frame_position += 16;
			absolute_position += 16;
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
		/// Reads the rest of the message bytes from the current position to the end.
		/// >1 Byte.
		/// 1 or more frames.
		/// </summary>
		/// <returns>Message bytes read.</returns>
		public byte[] ReadToEnd() {
			var remaining = Length - absolute_position;

			// If there is nothing left to read, return null; otherwise return the bytes.
			return remaining == 0 ? null : ReadBytes(remaining);
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
				var max_read_length = current_frame.DataLength - frame_position;
				var read_length = count - total_read < max_read_length ? count - total_read : max_read_length;
				// If we are at the end of this max frame size, get a new one.
				if (max_read_length == 0) {
					NextNonEmptyFrame();
					continue;
				}

				var read = current_frame.Read(frame_position, byte_buffer, offset, read_length);
				frame_position += read;
				absolute_position += read;
				total_read += read;
				offset += read;
			}

			return total_read;
		}

		/// <summary>
		/// Skips the next number of specified bytes.
		/// </summary>
		/// <param name="count">Number of bytes to skip reading.</param>
		public void Skip(int count) {
			var total_read = 0;
			var offset = 0;
			while (offset < count) {
				var max_read_length = current_frame.DataLength - frame_position;
				var read_length = count - total_read < max_read_length ? count - total_read : max_read_length;
				// If we are at the end of this max frame size, get a new one.
				if (max_read_length == 0) {
					NextNonEmptyFrame();
					continue;
				}

				frame_position += read_length;
				absolute_position += read_length;
				total_read += read_length;
				offset += read_length;
			}
		}


	}
}
