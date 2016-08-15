using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {

	/// <summary>
	/// Builder to aid in the creation of messages and their frames.
	/// </summary>
	public class MqMessageBuilder {
		private int position = 0;

		private readonly List<MqFrame> frames = new List<MqFrame>();

		private readonly MqFrame builder_frame;


		public MqMessageBuilder() { 
			builder_frame = new MqFrame(new byte[MqFrame.MaxFrameSize], MqFrameType.More);
		}

		private void EnsureSpace(int length) {
			// If this new requested length is outside our frame limit, copy the bytes from the builder frame to the actual final frame.
			if (position + length > builder_frame.DataLength) {
				InternalFinalizeFrame();
			}
		}


		public void FinalizeFrame() {
			if (position == 0) {
				frames.Add(new MqFrame(null, MqFrameType.Empty));
			} else {
				InternalFinalizeFrame();
			}
		}

		private void InternalFinalizeFrame() {
			if (position == 0) {
				throw new InvalidOperationException("Can not finalize frame when it is empty.");
			}
			var bytes = new byte[position];
			var frame = new MqFrame(bytes, MqFrameType.Last);
			Buffer.BlockCopy(builder_frame.Buffer, 0, bytes, 0, position);

			frames.Add(frame);

			position = 0;
		}

		/// <summary>
		/// Writes a boolean value.
		/// 1 Byte.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(bool value) {
			EnsureSpace(1);
			builder_frame.Write(position, value);
			position += 1;
		}

		/// <summary>
		/// Writes a byte value.
		/// 1 Byte.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(byte value) {
			EnsureSpace(1);
			builder_frame.Write(position, value);
			position += 1;
		}


		/// <summary>
		/// Writes a sbyte value.
		/// 1 Byte.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(sbyte value) {
			EnsureSpace(1);
			builder_frame.Write(position, value);
			position += 1;
		}


		/// <summary>
		/// Writes a short value.
		/// 2 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(short value) {
			EnsureSpace(2);
			builder_frame.Write(position, value);
			position += 2;
		}


		/// <summary>
		/// Writes a short value.
		/// 2 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(ushort value) {
			EnsureSpace(2);
			builder_frame.Write(position, value);
			position += 2;
		}




		/// <summary>
		/// Writes a int value.
		/// 4 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(int value) {
			EnsureSpace(4);
			builder_frame.Write(position, value);
			position += 4;
		}

		/// <summary>
		/// Writes a uint value.
		/// 4 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(uint value) {
			EnsureSpace(4);
			builder_frame.Write(position, value);
			position += 4;
		}


		/// <summary>
		/// Writes a long value.
		/// 8 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(long value) {
			EnsureSpace(8);
			builder_frame.Write(position, value);
			position += 8;
		}


		/// <summary>
		/// Writes a ulong value.
		/// 8 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(ulong value) {
			EnsureSpace(8);
			builder_frame.Write(position, value);
			position += 8;
		}



		/// <summary>
		/// Writes a float value.
		/// 4 Bytes.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(float value) {
			EnsureSpace(4);
			builder_frame.Write(position, value);
			position += 4;
		}


		/// <summary>
		/// Writes a double value.
		/// 8 Byte.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(double value) {
			EnsureSpace(8);
			builder_frame.Write(position, value);
			position += 8;
		}


		/// <summary>
		/// Writes a decimal value.
		/// 16 Byte.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(decimal value) {
			EnsureSpace(16);
			builder_frame.Write(position, value);
			position += 16;
		}

		
		/// <summary>
		/// Writes a char value.
		/// 1 Byte.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(char value) {
			EnsureSpace(1);
			builder_frame.Write(position, value);
			position += 1;
		}


		/// <summary>
		/// Appends an existing message to this message.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(MqMessage value) {
			InternalFinalizeFrame();
			frames.AddRange(value.Frames);
		}

		/// <summary>
		/// Writes a whole frame to the message.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(MqFrame value) {
			InternalFinalizeFrame();
			frames.Add(value);
		}

		/// <summary>
		/// Writes an empty frame to the message.
		/// </summary>
		public void Write() {
			InternalFinalizeFrame();
			frames.Add(new MqFrame(null, MqFrameType.Empty));
		}

		/// <summary>
		/// Writes a string.
		/// >4 Bytes.
		/// 1 or more frames.
		/// </summary>
		/// <param name="value">Value to write to the message.</param>
		public void Write(string value) {
			var string_bytes = Encoding.UTF8.GetBytes(value);

			// Write the length prefix
			EnsureSpace(4);
			builder_frame.Write(position, string_bytes.Length);
			position += 4;

			// Write the buffer to the message.
			Write(string_bytes, 0, string_bytes.Length);
			
		}

		/// <summary>
		/// Writes a byte array to this one or more frames.
		/// </summary>
		/// <param name="buffer">Buffer to write to the message.</param>
		/// <param name="offset">Offset in the buffer to write from</param>
		/// <param name="count">Number of bytes to write to the message from the buffer.</param>
		public void Write(byte[] buffer, int offset, int count) {
			int buffer_left = count;
			while (buffer_left > 0) {
				var max_write_length = builder_frame.DataLength - position;
				var write_length = max_write_length < buffer_left ? max_write_length : buffer_left;

				// If we are at the end of this max frame size, finalize it and start a new one.
				if (max_write_length == 0) {
					InternalFinalizeFrame();
					continue;
				}

				builder_frame.Write(position, buffer, offset, write_length);
				position += write_length;
				offset += write_length;
				buffer_left -= write_length;

				//return;
			}
		}



		/// <summary>
		/// Collects all the generated frames and outputs them as a single message.
		/// </summary>
		/// <returns>Message containing all frames.</returns>
		public MqMessage ToMessage() {
			return ToMessage(false);
		}

		/// <summary>
		/// Collects all the generated frames and outputs them as a single message.
		/// </summary>
		/// <param name="clear_builder">Optionally clear this builder and prepare for a new message.</param>
		/// <returns>Message containing all frames.</returns>
		public MqMessage ToMessage(bool clear_builder) {
			FinalizeFrame();
			var message = new MqMessage();
			message.AddRange(frames);
			message.PrepareSend();

			if (clear_builder) {
				Clear();
			}

			return message;

		}

		/// <summary>
		/// Clears and resets this builder for a new message.
		/// </summary>
		public void Clear() {
			frames.Clear();
			position = 0;
		}




	}
}
