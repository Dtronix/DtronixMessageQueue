using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Builder to aid in the creation of messages and their frames.
    /// </summary>
    public class MqMessageWriter : BinaryWriter
    {
        /// <summary>
        /// Encoding used for chars and strings
        /// </summary>
        private readonly Encoding _encoding;

        /// <summary>
        /// Internal set position for the builder_frame.
        /// </summary>
        private int _position;

        /// <summary>
        /// List of frames for the message.
        /// </summary>
        private readonly List<MqFrame> _frames = new List<MqFrame>();

        /// <summary>
        /// Internal frame used to write data to and copy from.
        /// </summary>
        private readonly MqFrame _builderFrame;

        /// <summary>
        /// Unused.  Stream.Null
        /// </summary>
        public override Stream BaseStream { get; } = Stream.Null;

        /// <summary>
        /// Session configurations
        /// </summary>
        private readonly MqConfig _config;

        /// <summary>
        /// Creates a new message writer with the specified socket configurations default UTF8 encoding.
        /// </summary>
        /// <param name="config">Current socket configurations.  Used to create new frames.</param>
        public MqMessageWriter(MqConfig config) : this(config, Encoding.UTF8)
        {
        }


        /// <summary>
        /// Creates a new message writer with the specified socket configurations and encoding.
        /// </summary>
        /// <param name="config">Current socket configurations.  Used to create new frames.</param>
        /// <param name="encoding">Encoding to use for string and char parsing.</param>
        public MqMessageWriter(MqConfig config, Encoding encoding)
        {
            _config = config;
            _encoding = encoding;
            _builderFrame = new MqFrame(new byte[config.FrameBufferSize], MqFrameType.More, config);
        }

        /// <summary>
        /// Used to ensure there is enough space left in this frame to write to.
        /// </summary>
        /// <param name="length">Number of bytes to ensure are available to write to.</param>
        private void EnsureSpace(int length)
        {
            // If this new requested length is outside our frame limit, copy the bytes from the builder frame to the actual final frame.
            if (_position + length > _builderFrame.DataLength)
            {
                InternalFinalizeFrame();
            }
        }

        /// <summary>
        /// Closes the current frame.  If no data has been written is empty, creates an empty frame.
        /// </summary>
        public void FinalizeFrame()
        {
            if (_position == 0)
            {
                _frames.Add(new MqFrame(null, MqFrameType.Empty, _config));
            }
            else
            {
                InternalFinalizeFrame();
            }
        }

        /// <summary>
        /// Copies the current data in the builder_frame into a new frame of the correct size.  Resets position to 0.
        /// </summary>
        private void InternalFinalizeFrame()
        {
            if (_position == 0)
            {
                throw new InvalidOperationException("Can not finalize frame when it is empty.");
            }
            var bytes = new byte[_position];
            var frame = new MqFrame(bytes, MqFrameType.Last, _config);
            Buffer.BlockCopy(_builderFrame.Buffer, 0, bytes, 0, _position);

            _frames.Add(frame);

            _position = 0;
        }

        /// <summary>
        /// Writes a boolean value.
        /// 1 Byte.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(bool value)
        {
            EnsureSpace(1);
            _builderFrame.Write(_position, value);
            _position += 1;
        }

        /// <summary>
        /// Writes a byte value.
        /// 1 Byte.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(byte value)
        {
            EnsureSpace(1);
            _builderFrame.Write(_position, value);
            _position += 1;
        }


        /// <summary>
        /// Writes a sbyte value.
        /// 1 Byte.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(sbyte value)
        {
            EnsureSpace(1);
            _builderFrame.Write(_position, value);
            _position += 1;
        }


        /// <summary>
        /// Writes a char value.
        /// >=1 Byte.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(char value)
        {
            Write(new[] {value});
        }

        /// <summary>
        /// Writes a whole character array to this one or more frames.
        /// >=1 byte
        /// </summary>
        /// <param name="chars"></param>
        public override void Write(char[] chars)
        {
            byte[] bytes = _encoding.GetBytes(chars);
            Write(bytes);
        }

        /// <summary>
        /// Writes a character array to this one or more frames.
        /// >=1 Byte
        /// 1 or more frames.
        /// </summary>
        /// <param name="chars">Character array to write to the </param>
        /// <param name="index">Offset in the buffer to write from</param>
        /// <param name="count">Number of bytes to write to the message from the buffer.</param>
        public override void Write(char[] chars, int index, int count)
        {
            byte[] bytes = _encoding.GetBytes(chars, index, count);
            Write(bytes);
        }


        /// <summary>
        /// Writes a short value.
        /// 2 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(short value)
        {
            EnsureSpace(2);
            _builderFrame.Write(_position, value);
            _position += 2;
        }


        /// <summary>
        /// Writes a short value.
        /// 2 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(ushort value)
        {
            EnsureSpace(2);
            _builderFrame.Write(_position, value);
            _position += 2;
        }


        /// <summary>
        /// Writes a int value.
        /// 4 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(int value)
        {
            EnsureSpace(4);
            _builderFrame.Write(_position, value);
            _position += 4;
        }

        /// <summary>
        /// Writes a uint value.
        /// 4 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(uint value)
        {
            EnsureSpace(4);
            _builderFrame.Write(_position, value);
            _position += 4;
        }


        /// <summary>
        /// Writes a long value.
        /// 8 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(long value)
        {
            EnsureSpace(8);
            _builderFrame.Write(_position, value);
            _position += 8;
        }


        /// <summary>
        /// Writes a ulong value.
        /// 8 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(ulong value)
        {
            EnsureSpace(8);
            _builderFrame.Write(_position, value);
            _position += 8;
        }


        /// <summary>
        /// Writes a float value.
        /// 4 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(float value)
        {
            EnsureSpace(4);
            _builderFrame.Write(_position, value);
            _position += 4;
        }


        /// <summary>
        /// Writes a double value.
        /// 8 Byte.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(double value)
        {
            EnsureSpace(8);
            _builderFrame.Write(_position, value);
            _position += 8;
        }


        /// <summary>
        /// Writes a decimal value.
        /// 16 Byte.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(decimal value)
        {
            EnsureSpace(16);
            _builderFrame.Write(_position, value);
            _position += 16;
        }


        /// <summary>
        /// Writes a string.
        /// >4 Bytes.
        /// 1 or more frames.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public override void Write(string value)
        {
            var stringBytes = Encoding.UTF8.GetBytes(value);

            // Write the length prefix
            EnsureSpace(4);
            _builderFrame.Write(_position, stringBytes.Length);
            _position += 4;

            // Write the buffer to the message.
            Write(stringBytes, 0, stringBytes.Length);
        }


        /// <summary>
        /// Writes a Guid.
        /// 16 Bytes.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public void Write(Guid value)
        {
            EnsureSpace(16);
            _builderFrame.Write(_position, value);
            _position += 16;
        }

        /// <summary>
        /// Appends an existing message to this message.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public void Write(MqMessage value)
        {
            InternalFinalizeFrame();
            _frames.AddRange(value);
        }

        /// <summary>
        /// Writes a whole frame to the message.
        /// </summary>
        /// <param name="value">Value to write to the message.</param>
        public void Write(MqFrame value)
        {
            InternalFinalizeFrame();
            _frames.Add(value);
        }

        /// <summary>
        /// Writes an empty frame to the message.
        /// </summary>
        public void Write()
        {
            InternalFinalizeFrame();
            _frames.Add(new MqFrame(null, MqFrameType.Empty, _config));
        }

        /// <summary>
        /// Writes a byte array to this one or more frames.
        /// >1 Byte.
        /// 1 or more frames.
        /// </summary>
        /// <param name="buffer">Buffer to write to the message.</param>
        /// <param name="index">Offset in the buffer to write from</param>
        /// <param name="count">Number of bytes to write to the message from the buffer.</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            int bufferLeft = count;
            while (bufferLeft > 0)
            {
                var maxWriteLength = _builderFrame.DataLength - _position;
                var writeLength = maxWriteLength < bufferLeft ? maxWriteLength : bufferLeft;

                // If we are at the end of this max frame size, finalize it and start a new one.
                if (maxWriteLength == 0)
                {
                    InternalFinalizeFrame();
                    continue;
                }

                _builderFrame.Write(_position, buffer, index, writeLength);
                _position += writeLength;
                index += writeLength;
                bufferLeft -= writeLength;

                //return;
            }
        }


        /// <summary>
        /// Writes a whole byte array to this one or more frames.
        /// </summary>
        /// <param name="buffer">Buffer to write to the message.</param>
        public override void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }


        /// <summary>
        /// Seeking is disabled.
        /// </summary>
        /// <param name="offset">N/A</param>
        /// <param name="origin">N/A</param>
        /// <returns>N/A</returns>
        public override long Seek(int offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Collects all the generated frames and outputs them as a single message.
        /// </summary>
        /// <returns>Message containing all frames.</returns>
        public MqMessage ToMessage()
        {
            return ToMessage(false);
        }

        /// <summary>
        /// Collects all the generated frames and outputs them as a single message.
        /// </summary>
        /// <param name="clearBuilder">Optionally clear this builder and prepare for a new message.</param>
        /// <returns>Message containing all frames.</returns>
        public MqMessage ToMessage(bool clearBuilder)
        {
            FinalizeFrame();
            var message = new MqMessage();
            message.AddRange(_frames);
            message.PrepareSend();

            if (clearBuilder)
            {
                Clear();
            }

            return message;
        }

        /// <summary>
        /// Clears and resets this builder for a new message.
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
            _position = 0;
        }
    }
}