using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Reader to parse a message into sub-components.
    /// </summary>
    public class MqMessageReader : BinaryReader
    {
        /// <summary>
        /// Current encoding to use for char and string parsing.
        /// </summary>
        private readonly Encoding _encoding;

        /// <summary>
        /// Current decoder to use for char and string parsing.
        /// </summary>
        private readonly Decoder _decoder;

        /// <summary>
        /// Current position in the frame that is being read.
        /// </summary>
        private int _framePosition;

        /// <summary>
        /// Position in the entire message read.
        /// </summary>
        private int _absolutePosition;

        /// <summary>
        /// Current message being read.
        /// </summary>
        private MqMessage _message;

        /// <summary>
        /// Current frame being read.
        /// </summary>
        private MqFrame _currentFrame;

        /// <summary>
        /// Current frame in the message that is being read.
        /// </summary>
        private int _messagePosition;

        /// <summary>
        /// True if this encoding always has two bytes per character.
        /// </summary>
        private readonly bool _twoBytesPerChar;

        /// <summary>
        /// Maximum number of chars to read from the message.
        /// </summary>
        private const int MaxCharBytesSize = 128;

        /// <summary>
        /// Number of bytes each char takes for the specified encoding.
        /// </summary>
        private byte[] _charBytes;

        /// <summary>
        /// Current frame that is being read.
        /// </summary>
        public MqFrame CurrentFrame => _currentFrame;


        /// <summary>
        /// Gets the current message.
        /// Setting a message resets reading positions to the beginning.
        /// </summary>
        public MqMessage Message
        {
            get { return _message; }
            set
            {
                _message = value;
                _currentFrame = value?[0];
                _framePosition = 0;
                _messagePosition = 0;
                _absolutePosition = 0;
            }
        }

        /// <summary>
        /// Get or set the byte position in the message.
        /// </summary>
        public int Position
        {
            get { return _absolutePosition; }
            set
            {
                _currentFrame = _message?[0];
                _framePosition = 0;
                _messagePosition = 0;
                _absolutePosition = 0;
                if (value != 0)
                {
                    Skip(value);
                }
            }
        }

        /// <summary>
        /// Total length of the bytes in this message.
        /// </summary>
        public int Length => _message.Sum(frm => frm.DataLength);

        /// <summary>
        /// Unused. Stream.Null
        /// </summary>
        public override Stream BaseStream { get; } = Stream.Null;

        /// <summary>
        /// True if we are at the end of the last frame of the message.
        /// </summary>
        public bool IsAtEnd
        {
            get
            {
                var lastFrame = _message[_message.Count - 1];
                return _currentFrame == lastFrame && lastFrame.DataLength == _framePosition;
            }
        }


        /// <summary>
        /// Creates a new message reader with no message and the default encoding of UTF8.
        /// </summary>
        public MqMessageReader() : this(null)
        {
        }

        /// <summary>
        /// Creates a new message reader with the specified message to read and the default encoding of UTF8.
        /// </summary>
        /// <param name="initialMessage">Message to read.</param>
        public MqMessageReader(MqMessage initialMessage) : this(initialMessage, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Creates a new message reader with the specified message to read and the specified encoding.
        /// </summary>
        /// <param name="initialMessage">Message to read.</param>
        /// <param name="encoding">Encoding to use for string interpretation.</param>
        public MqMessageReader(MqMessage initialMessage, Encoding encoding) : base(Stream.Null)
        {
            _encoding = encoding;
            _decoder = _encoding.GetDecoder();
            _twoBytesPerChar = encoding is UnicodeEncoding;
            Message = initialMessage;
        }

        /// <summary>
        /// Ensures the specified number of bytes is available to read from.
        /// </summary>
        /// <param name="length">Number of bytes to ensure available.</param>
        // ReSharper disable once UnusedParameter.Local
        private void EnsureBuffer(int length)
        {
            if (_framePosition + length > _currentFrame.DataLength)
                throw new InvalidOperationException("Trying to read simple type across frames which is not allowed.");
        }

        /// <summary>
        /// Skips over to the next non-empty frame in the message.
        /// </summary>
        private void NextNonEmptyFrame()
        {
            _framePosition = 0;
            // Increment until we reach the next non-empty frame.
            do
            {
                _messagePosition++;
            } while (_message[_messagePosition]?.FrameType == MqFrameType.Empty);

            _currentFrame = _messagePosition >= _message.Count ? null : _message[_messagePosition];
        }

        /// <summary>
        /// Advances the reader to the next frame and resets reading positions.
        /// </summary>
        public void NextFrame()
        {
            _framePosition = 0;
            _messagePosition += 1;
            _currentFrame = _message[_messagePosition];
        }

        /// <summary>
        /// Reads a boolean value.
        /// 1 Byte.
        /// </summary>
        public override bool ReadBoolean()
        {
            EnsureBuffer(1);
            var value = _currentFrame.ReadBoolean(_framePosition);
            _framePosition += 1;
            _absolutePosition += 1;
            return value;
        }


        /// <summary>
        /// Reads a byte value.
        /// 1 Byte
        /// </summary>
        public override byte ReadByte()
        {
            EnsureBuffer(1);
            var value = _currentFrame.ReadByte(_framePosition);
            _framePosition += 1;
            _absolutePosition += 1;
            return value;
        }

        /// <summary>
        /// Reads a sbyte value.
        /// 1 Byte
        /// </summary>
        public override sbyte ReadSByte()
        {
            EnsureBuffer(1);
            var value = _currentFrame.ReadSByte(_framePosition);
            _framePosition += 1;
            _absolutePosition += 1;
            return value;
        }


        /// <summary>
        /// Reads a char value.
        /// >=1 Byte.
        /// </summary>
        public override char ReadChar()
        {
            var readChar = new char[1];
            Read(readChar, 0, 1);
            return readChar[0];
        }


        /// <summary>
        /// Reads the specified number of chars from the message.
        /// >1 Byte.
        /// 1 or more frames.
        /// </summary>
        /// <param name="count">Number of chars to read.</param>
        /// <returns>Char array.</returns>
        public override char[] ReadChars(int count)
        {
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
        public override int Read(char[] buffer, int index, int count)
        {
            var charsRemaining = count;

            if (_charBytes == null)
            {
                _charBytes = new byte[MaxCharBytesSize];
            }

            while (charsRemaining > 0)
            {
                int charsRead;
                // We really want to know what the minimum number of bytes per char
                // is for our encoding.  Otherwise for UnicodeEncoding we'd have to
                // do ~1+log(n) reads to read n characters. 
                var numBytes = charsRemaining;


                // TODO: special case for UTF8Decoder when there are residual bytes from previous loop 
                /*UTF8Encoding.UTF8Decoder decoder = m_decoder as UTF8Encoding.UTF8Decoder;
                if (decoder != null && decoder.HasState && numBytes > 1) {
                    numBytes -= 1;
                }*/


                if (_twoBytesPerChar)
                {
                    numBytes <<= 1;
                }

                if (numBytes > MaxCharBytesSize)
                {
                    numBytes = MaxCharBytesSize;
                }

                var charPosition = 0;

                numBytes = Read(_charBytes, 0, numBytes);
                var byteBuffer = _charBytes;


                if (numBytes == 0)
                {
                    return count - charsRemaining;
                }

                unsafe
                {
                    fixed (byte* bytes = byteBuffer)
                    fixed (char* chars = buffer)
                    {
                        charsRead = _decoder.GetChars(bytes + charPosition, numBytes, chars + index, charsRemaining,
                            false);
                    }
                }

                charsRemaining -= charsRead;
                index += charsRead;
            }

            // we may have read fewer than the number of characters requested if end of stream reached 
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence)
            return count - charsRemaining;
        }

        /// <summary>
        /// Peeks at the next char value.
        /// >=1 Byte.
        /// </summary>
        public override int PeekChar()
        {
            // Store the temporary state of the reader.
            var previousFrame = _currentFrame;
            var previousPosition = _framePosition;
            var previousMessagePosition = _messagePosition;

            var value = ReadChar();

            // Restore the original state of the reader.
            _currentFrame = previousFrame;
            _framePosition = previousPosition;
            _messagePosition = previousMessagePosition;

            return value;
        }

        /// <summary>
        /// Reads a short value.
        /// 2 Bytes.
        /// </summary>
        public override short ReadInt16()
        {
            EnsureBuffer(2);
            var value = _currentFrame.ReadInt16(_framePosition);
            _framePosition += 2;
            _absolutePosition += 2;
            return value;
        }

        /// <summary>
        /// Reads a ushort value.
        /// 2 Bytes.
        /// </summary>
        public override ushort ReadUInt16()
        {
            EnsureBuffer(2);
            var value = _currentFrame.ReadUInt16(_framePosition);
            _framePosition += 2;
            _absolutePosition += 2;
            return value;
        }


        /// <summary>
        /// Reads a int value.
        /// 4 Bytes.
        /// </summary>
        public override int Read()
        {
            return ReadInt32();
        }


        /// <summary>
        /// Reads a int value.
        /// 4 Bytes.
        /// </summary>
        public override int ReadInt32()
        {
            EnsureBuffer(4);
            var value = _currentFrame.ReadInt32(_framePosition);
            _framePosition += 4;
            _absolutePosition += 4;
            return value;
        }


        /// <summary>
        /// Reads a uint value.
        /// 4 Bytes.
        /// </summary>
        public override uint ReadUInt32()
        {
            EnsureBuffer(4);
            var value = _currentFrame.ReadUInt32(_framePosition);
            _framePosition += 4;
            _absolutePosition += 4;
            return value;
        }

        /// <summary>
        /// Reads a long value.
        /// 8 Bytes.
        /// </summary>
        public override long ReadInt64()
        {
            EnsureBuffer(8);
            var value = _currentFrame.ReadInt64(_framePosition);
            _framePosition += 8;
            _absolutePosition += 8;
            return value;
        }


        /// <summary>
        /// Reads a ulong value.
        /// 8 Bytes.
        /// </summary>
        public override ulong ReadUInt64()
        {
            EnsureBuffer(8);
            var value = _currentFrame.ReadUInt64(_framePosition);
            _framePosition += 8;
            _absolutePosition += 8;
            return value;
        }


        /// <summary>
        /// Reads a float value.
        /// 4 Bytes.
        /// </summary>
        public override float ReadSingle()
        {
            EnsureBuffer(4);
            var value = _currentFrame.ReadSingle(_framePosition);
            _framePosition += 4;
            _absolutePosition += 4;
            return value;
        }


        /// <summary>
        /// Reads a double value.
        /// 8 Bytes.
        /// </summary>
        public override double ReadDouble()
        {
            EnsureBuffer(8);
            var value = _currentFrame.ReadDouble(_framePosition);
            _framePosition += 8;
            _absolutePosition += 8;
            return value;
        }

        /// <summary>
        /// Reads a decimal value.
        /// 16 Bytes.
        /// </summary>
        public override decimal ReadDecimal()
        {
            EnsureBuffer(16);
            var value = _currentFrame.ReadDecimal(_framePosition);
            _framePosition += 16;
            _absolutePosition += 16;
            return value;
        }

        /// <summary>
        /// Reads a string.
        /// >4 Bytes.
        /// 1 or more frames.
        /// </summary>
        public override string ReadString()
        {
            // Write the length prefix

            var strLen = ReadInt32();
            var strBuffer = new byte[strLen];
            Read(strBuffer, 0, strLen);

            return _encoding.GetString(strBuffer);
        }

        /// <summary>
        /// Reads the rest of the message bytes from the current position to the end.
        /// >1 Byte.
        /// 1 or more frames.
        /// </summary>
        /// <returns>Message bytes read.</returns>
        public byte[] ReadToEnd()
        {
            var remaining = Length - _absolutePosition;

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
        public override byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            Read(bytes, 0, count);
            return bytes;
        }

        /// <summary>
        /// Reads the bytes from this message.
        /// </summary>
        /// <param name="byteBuffer">Buffer to copy the frame bytes to.</param>
        /// <param name="offset">Offset in the byte buffer to copy the frame bytes to.</param>
        /// <param name="count">Number of bytes to try to copy.</param>
        /// <returns>Actual number of bytes read.  May be less than the number requested due to being at the end of the frame.</returns>
        public override int Read(byte[] byteBuffer, int offset, int count)
        {
            var totalRead = 0;
            while (offset < count)
            {
                var maxReadLength = _currentFrame.DataLength - _framePosition;
                var readLength = count - totalRead < maxReadLength ? count - totalRead : maxReadLength;
                // If we are at the end of this max frame size, get a new one.
                if (maxReadLength == 0)
                {
                    NextNonEmptyFrame();
                    continue;
                }

                var read = _currentFrame.Read(_framePosition, byteBuffer, offset, readLength);
                _framePosition += read;
                _absolutePosition += read;
                totalRead += read;
                offset += read;
            }

            return totalRead;
        }

        /// <summary>
        /// Skips the next number of specified bytes.
        /// </summary>
        /// <param name="count">Number of bytes to skip reading.</param>
        public void Skip(int count)
        {
            var totalRead = 0;
            var offset = 0;
            while (offset < count)
            {
                var maxReadLength = _currentFrame.DataLength - _framePosition;
                var readLength = count - totalRead < maxReadLength ? count - totalRead : maxReadLength;
                // If we are at the end of this max frame size, get a new one.
                if (maxReadLength == 0)
                {
                    NextNonEmptyFrame();
                    continue;
                }

                _framePosition += readLength;
                _absolutePosition += readLength;
                totalRead += readLength;
                offset += readLength;
            }
        }
    }
}