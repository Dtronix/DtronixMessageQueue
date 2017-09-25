using System;
using System.Text;

namespace DtronixMessageQueue
{
    /// <summary>
    /// byte_buffer frame containing raw byte_buffer to be sent over the transport.
    /// </summary>
    public class MqFrame : IDisposable
    {
        private byte[] _buffer;
        private MqFrameType _frameType;

        /// <summary>
        /// Information about this frame and how it relates to other Frames.
        /// </summary>
        public MqFrameType FrameType
        {
            get { return _frameType; }
            set
            {
                if (value == MqFrameType.EmptyLast || value == MqFrameType.Empty)
                {
                    if (_buffer != null && _buffer.Length != 0)
                    {
                        throw new ArgumentException($"Can not set frame to {value} when buffer is not null or empty.");
                    }
                }
                _frameType = value;
            }
        }

        /// <summary>
        /// Size taken up with the header for this frame.
        /// </summary>
        public const int HeaderLength = 3;

        /// <summary>
        /// Contains the configuration information about the current client/server.
        /// Used to determine how large the frames are to be.
        /// </summary>
        private MqConfig _config;

        /// <summary>
        /// Total bytes that this frame contains.
        /// </summary>
        public int DataLength => _buffer?.Length ?? 0;

        /// <summary>
        /// Bytes this frame contains.
        /// </summary>
        public byte[] Buffer => _buffer;

        /// <summary>
        /// Total number of bytes that compose this raw frame.
        /// </summary>
        public int FrameSize
        {
            get
            {
                // If this frame is empty, then it has a total of one byte.
                if (FrameType == MqFrameType.Empty || FrameType == MqFrameType.EmptyLast ||
                    FrameType == MqFrameType.Ping)
                {
                    return 1;
                }

                return HeaderLength + DataLength;
            }
        }


        /// <summary>
        /// Creates a new frame wit the specified bytes and configurations.
        /// </summary>
        /// <param name="bytes">Byte buffer to use for this frame.</param>
        /// <param name="config">Socket configurations used for creating and finalizing this frame.</param>
        public MqFrame(byte[] bytes, MqConfig config) : this(bytes, MqFrameType.Unset, config)
        {
        }

        /// <summary>
        /// Creates a new frame wit the specified bytes and configurations.
        /// </summary>
        /// <param name="bytes">Byte buffer to use for this frame.</param>
        /// <param name="type">Initial type of frame to create.</param>
        /// <param name="config">Socket configurations used for creating and finalizing this frame.</param>
        public MqFrame(byte[] bytes, MqFrameType type, MqConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configurations can not be null.");
            }

            _config = config;
            if (bytes?.Length > _config.FrameBufferSize)
            {
                throw new ArgumentException(
                    "Byte array passed is larger than the maximum frame size allowed.  Must be less than " +
                    config.FrameBufferSize,
                    nameof(bytes));
            }
            _buffer = bytes;
            FrameType = type;
        }


        /// <summary>
        /// Sets this frame to be the last frame of a message.
        /// </summary>
        public void SetLast()
        {
            if (FrameType != MqFrameType.Command &&
                FrameType != MqFrameType.Ping)
            {
                FrameType = FrameType == MqFrameType.Empty ? MqFrameType.EmptyLast : MqFrameType.Last;
            }
        }

        /// <summary>
        /// Returns this frame as a raw byte array. (Header + byte_buffer)
        /// </summary>
        /// <returns>Frame bytes.</returns>
        public byte[] RawFrame()
        {
            // If this is an empty frame, return an empty byte which corresponds to MqFrameType.Empty or MqFrameType.EmptyLast
            if (FrameType == MqFrameType.Empty || FrameType == MqFrameType.EmptyLast || FrameType == MqFrameType.Ping)
            {
                return new[] {(byte) FrameType};
            }

            if (_buffer == null)
            {
                throw new InvalidOperationException("Can not retrieve frame bytes when frame has not been created.");
            }

            var bytes = new byte[HeaderLength + _buffer.Length];

            // Type of frame that this is.
            bytes[0] = (byte) FrameType;

            var sizeBytes = BitConverter.GetBytes((short) _buffer.Length);

            // Copy the length.
            System.Buffer.BlockCopy(sizeBytes, 0, bytes, 1, 2);

            // Copy the byte_buffer
            System.Buffer.BlockCopy(_buffer, 0, bytes, HeaderLength, _buffer.Length);

            return bytes;
        }

        /// <summary>
        /// Gets or sets the frame byte at the specified index.
        /// </summary>
        /// <returns>The byte at the specified index.</returns>
        /// <param name="index">The zero-based index of the byte to get or set.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the message.</exception>
        public byte this[int index]
        {
            get { return _buffer[index]; }
            set { _buffer[index] = value; }
        }

        /// <summary>
        /// Reads a boolean value at the specified index.
        /// 1 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public bool ReadBoolean(int index)
        {
            return _buffer[index] != 0;
        }

        /// <summary>
        /// Writes a boolean value at the specified index.
        /// 1 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, bool value)
        {
            _buffer[index] = (byte) (value ? 1 : 0);
        }

        /// <summary>
        /// Reads a byte value at the specified index.
        /// 1 Byte
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public byte ReadByte(int index)
        {
            return _buffer[index];
        }

        /// <summary>
        /// Writes a byte value at the specified index.
        /// 1 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, byte value)
        {
            _buffer[index] = value;
        }


        /// <summary>
        /// Reads a sbyte value at the specified index.
        /// 1 Byte
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public sbyte ReadSByte(int index)
        {
            return (sbyte) _buffer[index];
        }

        /// <summary>
        /// Writes a sbyte value at the specified index.
        /// 1 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, sbyte value)
        {
            _buffer[index] = (byte) value;
        }

        /// <summary>
        /// Reads a char value at the specified index.
        /// 1 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public char ReadChar(int index)
        {
            return (char) _buffer[index];
        }

        /// <summary>
        /// Writes a char value at the specified index.
        /// 1 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, char value)
        {
            _buffer[index] = (byte) value;
        }

        /// <summary>
        /// Reads a short value at the specified index.
        /// 2 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public short ReadInt16(int index)
        {
            return (short) (_buffer[index] | _buffer[index + 1] << 8);
        }


        /// <summary>
        /// Writes a short value at the specified index.
        /// 2 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, short value)
        {
            _buffer[index + 0] = (byte) value;
            _buffer[index + 1] = (byte) (value >> 8);
        }

        /// <summary>
        /// Reads a ushort value at the specified index.
        /// 2 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public ushort ReadUInt16(int index)
        {
            return (ushort) (_buffer[index] | _buffer[index + 1] << 8);
        }


        /// <summary>
        /// Writes a short value at the specified index.
        /// 2 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, ushort value)
        {
            _buffer[index + 0] = (byte) value;
            _buffer[index + 1] = (byte) (value >> 8);
        }


        /// <summary>
        /// Reads a int value at the specified index.
        /// 4 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public int ReadInt32(int index)
        {
            return _buffer[index] | _buffer[index + 1] << 8 | _buffer[index + 2] << 16 | _buffer[index + 3] << 24;
        }


        /// <summary>
        /// Writes a int value at the specified index.
        /// 4 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, int value)
        {
            _buffer[index + 0] = (byte) value;
            _buffer[index + 1] = (byte) (value >> 8);
            _buffer[index + 2] = (byte) (value >> 16);
            _buffer[index + 3] = (byte) (value >> 24);
        }

        /// <summary>
        /// Reads a uint value at the specified index.
        /// 4 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public uint ReadUInt32(int index)
        {
            return
                (uint) (_buffer[index] | _buffer[index + 1] << 8 | _buffer[index + 2] << 16 | _buffer[index + 3] << 24);
        }

        /// <summary>
        /// Writes a uint value at the specified index.
        /// 4 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, uint value)
        {
            _buffer[index + 0] = (byte) value;
            _buffer[index + 1] = (byte) (value >> 8);
            _buffer[index + 2] = (byte) (value >> 16);
            _buffer[index + 3] = (byte) (value >> 24);
        }

        /// <summary>
        /// Reads a long value at the specified index.
        /// 8 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public long ReadInt64(int index)
        {
            var lo = (uint) (_buffer[index] | _buffer[index + 1] << 8 |
                             _buffer[index + 2] << 16 | _buffer[index + 3] << 24);
            var hi = (uint) (_buffer[index + 4] | _buffer[index + 5] << 8 |
                             _buffer[index + 6] << 16 | _buffer[index + 7] << 24);
            return (long) hi << 32 | lo;
        }

        /// <summary>
        /// Writes a long value at the specified index.
        /// 8 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, long value)
        {
            _buffer[index + 0] = (byte) value;
            _buffer[index + 1] = (byte) (value >> 8);
            _buffer[index + 2] = (byte) (value >> 16);
            _buffer[index + 3] = (byte) (value >> 24);
            _buffer[index + 4] = (byte) (value >> 32);
            _buffer[index + 5] = (byte) (value >> 40);
            _buffer[index + 6] = (byte) (value >> 48);
            _buffer[index + 7] = (byte) (value >> 56);
        }

        /// <summary>
        /// Reads a ulong value at the specified index.
        /// 8 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public ulong ReadUInt64(int index)
        {
            var lo = (uint) (_buffer[index] | _buffer[index + 1] << 8 |
                             _buffer[index + 2] << 16 | _buffer[index + 3] << 24);
            var hi = (uint) (_buffer[index + 4] | _buffer[index + 5] << 8 |
                             _buffer[index + 6] << 16 | _buffer[index + 7] << 24);
            return (ulong) hi << 32 | lo;
        }

        /// <summary>
        /// Writes a ulong value at the specified index.
        /// 8 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, ulong value)
        {
            _buffer[index + 0] = (byte) value;
            _buffer[index + 1] = (byte) (value >> 8);
            _buffer[index + 2] = (byte) (value >> 16);
            _buffer[index + 3] = (byte) (value >> 24);
            _buffer[index + 4] = (byte) (value >> 32);
            _buffer[index + 5] = (byte) (value >> 40);
            _buffer[index + 6] = (byte) (value >> 48);
            _buffer[index + 7] = (byte) (value >> 56);
        }

        /// <summary>
        /// Reads a float value at the specified index.
        /// 4 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public unsafe float ReadSingle(int index)
        {
            var tmpBuffer =
                (uint) (_buffer[index] | _buffer[index + 1] << 8 | _buffer[index + 2] << 16 | _buffer[index + 3] << 24);
            return *(float*) &tmpBuffer;
        }


        /// <summary>
        /// Writes a float value at the specified index.
        /// 4 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public unsafe void Write(int index, float value)
        {
            var tmpValue = *(uint*) &value;
            _buffer[index + 0] = (byte) tmpValue;
            _buffer[index + 1] = (byte) (tmpValue >> 8);
            _buffer[index + 2] = (byte) (tmpValue >> 16);
            _buffer[index + 3] = (byte) (tmpValue >> 24);
        }


        /// <summary>
        /// Reads a double value at the specified index.
        /// 8 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public unsafe double ReadDouble(int index)
        {
            var lo = (uint) (_buffer[index] | _buffer[index + 1] << 8 |
                             _buffer[index + 2] << 16 | _buffer[index + 3] << 24);
            var hi = (uint) (_buffer[index + 4] | _buffer[index + 5] << 8 |
                             _buffer[index + 6] << 16 | _buffer[index + 7] << 24);

            var tmpBuffer = (ulong) hi << 32 | lo;
            return *(double*) &tmpBuffer;
        }


        /// <summary>
        /// Writes a double value at the specified index.
        /// 8 Byte.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public unsafe void Write(int index, double value)
        {
            var tmpValue = *(ulong*) &value;
            _buffer[index + 0] = (byte) tmpValue;
            _buffer[index + 1] = (byte) (tmpValue >> 8);
            _buffer[index + 2] = (byte) (tmpValue >> 16);
            _buffer[index + 3] = (byte) (tmpValue >> 24);
            _buffer[index + 4] = (byte) (tmpValue >> 32);
            _buffer[index + 5] = (byte) (tmpValue >> 40);
            _buffer[index + 6] = (byte) (tmpValue >> 48);
            _buffer[index + 7] = (byte) (tmpValue >> 56);
        }

        /// <summary>
        /// Reads a decimal value at the specified index.
        /// 16 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public decimal ReadDecimal(int index)
        {
            var ints = new int[4];
            System.Buffer.BlockCopy(_buffer, index, ints, 0, 16);
            try
            {
                return new decimal(ints);
            }
            catch (ArgumentException e)
            {
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
        public void Write(int index, decimal value)
        {
            var bits = decimal.GetBits(value);

            var lo = bits[0];
            _buffer[index + 0] = (byte) lo;
            _buffer[index + 1] = (byte) (lo >> 8);
            _buffer[index + 2] = (byte) (lo >> 16);
            _buffer[index + 3] = (byte) (lo >> 24);

            var mid = bits[1];
            _buffer[index + 4] = (byte) mid;
            _buffer[index + 5] = (byte) (mid >> 8);
            _buffer[index + 6] = (byte) (mid >> 16);
            _buffer[index + 7] = (byte) (mid >> 24);

            var hi = bits[2];
            _buffer[index + 8] = (byte) hi;
            _buffer[index + 9] = (byte) (hi >> 8);
            _buffer[index + 10] = (byte) (hi >> 16);
            _buffer[index + 11] = (byte) (hi >> 24);

            var flags = bits[3];
            _buffer[index + 12] = (byte) flags;
            _buffer[index + 13] = (byte) (flags >> 8);
            _buffer[index + 14] = (byte) (flags >> 16);
            _buffer[index + 15] = (byte) (flags >> 24);
        }


        /// <summary>
        /// Reads a ASCII encoded string value at the specified index.
        /// (uint16)(string)
        /// >2 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        /// <param name="stringLength">Specified length of the ASCII string.  Default of -1 means the string is prepended with the length which should be read first automatically.</param>
        public string ReadAscii(int index, int stringLength = -1)
        {
            var prependedLength = stringLength == -1;
            if (prependedLength)
            {
                stringLength = ReadUInt16(index);
            }

            var strBuffer = new byte[stringLength];
            Read(index + (prependedLength ? 2 : 0), strBuffer, 0, stringLength);

            return Encoding.ASCII.GetString(strBuffer);
        }


        /// <summary>
        /// Writes a ASCII encoded string.
        /// (uint16?)(string)
        /// >2|1 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        /// <param name="prependSize">If set to true, the written string will follow a uint16 of the length of the encoded string.  Otherwise it will be just an encoded string.</param>
        public void WriteAscii(int index, string value, bool prependSize = true)
        {
            var stringBytes = Encoding.ASCII.GetBytes(value);

            if (prependSize)
            {
                Write(index, (ushort)stringBytes.Length);
            }

            if (index + stringBytes.Length + (prependSize ? 2 : 0) > _config.FrameBufferSize)
            {
                throw new InvalidOperationException("Length of ASCII string is longer than the frame will allow.");
            }

            Write(index + (prependSize ? 2 : 0), stringBytes, 0, stringBytes.Length);
        }

        /// <summary>
        /// Writes a Guid byte array value.
        /// 16 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to write the value to.</param>
        /// <param name="value">Value to write to the specified index.</param>
        public void Write(int index, Guid value)
        {
            var guidBytes = value.ToByteArray();

            Write(index, guidBytes, 0, guidBytes.Length);
        }

        /// <summary>
        /// Reads a Guid value at the specified index.
        /// 16 Bytes.
        /// </summary>
        /// <param name="index">The zero-based index to read the value from.</param>
        public Guid ReadGuid(int index)
        {
            var guidBuffer = new byte[16];
            Read(index, guidBuffer, 0, 16);
            return new Guid(guidBuffer);
        }


        /// <summary>
        /// Reads the bytes from this frame.
        /// </summary>
        /// <param name="frameIndex">Bytes to start reading from.</param>
        /// <param name="byteBuffer">Buffer to copy the frame bytes to.</param>
        /// <param name="offset">Offset in the byte buffer to copy the frame bytes to.</param>
        /// <param name="count">Number of bytes to try to copy.</param>
        /// <returns>Actual number of bytes read.  May be less than the number requested due to being at the end of the frame.</returns>
        public int Read(int frameIndex, byte[] byteBuffer, int offset, int count)
        {
            if (byteBuffer == null)
            {
                throw new ArgumentNullException(nameof(byteBuffer), "Buffer is null.");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Need positive offset.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Need positive number");
            }
            if (byteBuffer.Length - offset < count)
            {
                throw new ArgumentException("Invalid offset length.");
            }

            var maxLen = _buffer.Length - frameIndex;
            var reqLen = count;

            count = maxLen < reqLen ? maxLen : reqLen;

            System.Buffer.BlockCopy(_buffer, frameIndex, byteBuffer, offset, count);

            return count;
        }

        /// <summary>
        /// Writes a byte buffer to this frame.
        /// </summary>
        /// <param name="frameIndex">Start position in this frame to write to.</param>
        /// <param name="byteBuffer">Buffer to write to this frame.</param>
        /// <param name="offset">Offset in the byte buffer.</param>
        /// <param name="count">Number of bytes to copy to this frame.</param>
        public void Write(int frameIndex, byte[] byteBuffer, int offset, int count)
        {
            System.Buffer.BlockCopy(byteBuffer, offset, _buffer, frameIndex, count);
        }

        /// <summary>
        /// Creates a string representation of this frame.
        /// </summary>
        /// <returns>A string representation of this frame.</returns>
        public override string ToString()
        {
            return $"MqFrame totaling {_buffer.Length:N0} bytes; Type: {FrameType}";
        }

        /// <summary>
        /// Puts this frame inside a message.
        /// </summary>
        /// <returns>Message with this frame as the first frame.</returns>
        public MqMessage ToMessage()
        {
            return new MqMessage(this);
        }

        /// <summary>
        /// Disposes of this object and its resources.
        /// </summary>
        public void Dispose()
        {
            _buffer = null;
        }

        /// <summary>
        /// Shallow copies this frame into a new frame.
        /// </summary>
        /// <returns>Shallow copied frame.</returns>
        public MqFrame ShallowCopy()
        {
            return new MqFrame(_buffer, _frameType, _config);
        }


        /// <summary>
        /// Deep copies a frame into new buffer.
        /// </summary>
        /// <returns>Deep copied frame.</returns>
        public MqFrame Clone()
        {
            var newBuffer = new byte[_buffer.Length];
            System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);

            return new MqFrame(newBuffer, _frameType, _config);
        }
    }
}