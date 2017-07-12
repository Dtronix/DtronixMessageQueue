using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Class to parse raw byte arrays into frames.
    /// </summary>
    public class MqFrameBuilder : IDisposable
    {
        /// <summary>
        /// Byte buffer used to maintain and parse the passed buffers.
        /// </summary>
        private readonly byte[] _internalBuffer;

        /// <summary>
        /// Data for this frame
        /// </summary>
        private byte[] _currentFrameData;

        /// <summary>
        /// Current parsed type.  is Unknown by default.
        /// </summary>
        private MqFrameType _currentFrameType;

        /// <summary>
        /// Reading position in the internal buffer.  Set by the internal reader.
        /// </summary>
        private int _readPosition;

        /// <summary>
        /// Writing position in the internal buffer.  Set by the internal writer.
        /// </summary>
        private int _writePosition;

        /// <summary>
        /// Total length of the internal buffer's data.  Set by the reader and writer.
        /// </summary>
        private int _streamLength;

        /// <summary>
        /// Memory stream used to read and write to the internal buffer.
        /// </summary>
        private readonly MemoryStream _bufferStream;

        /// <summary>
        /// Used to cache the maximum size of the MqFrameType.
        /// </summary>
        private static int _maxTypeEnum = -1;

        /// <summary>
        /// Configurations for the connected session.
        /// </summary>
        private readonly MqConfig _config;

        /// <summary>
        /// Size in bytes of the header of a frame.
        /// MqFrameType:byte[1], Length:UInt16[2]
        /// </summary>
        public const int HeaderLength = 3;

        /// <summary>
        /// Parsed frames from the incoming stream.
        /// </summary>
        public Queue<MqFrame> Frames { get; } = new Queue<MqFrame>();

        /// <summary>
        /// Creates a new instance of the frame builder to handle parsing of incoming byte stream.
        /// </summary>
        /// <param name="config">Socket configurations for this session.</param>
        public MqFrameBuilder(MqConfig config)
        {
            _config = config;
            _internalBuffer = new byte[(config.FrameBufferSize + MqFrame.HeaderLength) * 2 ];

            // Determine what our max enum value is for the FrameType
            if (_maxTypeEnum == -1)
            {
                _maxTypeEnum = Enum.GetValues(typeof(MqFrameType)).Cast<byte>().Max();
            }

            _bufferStream = new MemoryStream(_internalBuffer, 0, _internalBuffer.Length, true, true);
        }

        /// <summary>
        /// Reads from the internal stream.
        /// </summary>
        /// <param name="buffer">Byte buffer to read into.</param>
        /// <param name="offset">Offset position in the buffer to copy from.</param>
        /// <param name="count">Number of bytes to attempt to read.</param>
        /// <returns>Total bytes that were read.</returns>
        private int ReadInternal(byte[] buffer, int offset, int count)
        {
            _bufferStream.Position = _readPosition;
            var length = _bufferStream.Read(buffer, offset, count);
            _readPosition += length;

            // Update the stream length 
            _streamLength = _writePosition - _readPosition;
            return length;
        }

        /// <summary>
        /// Writes to the internal stream from the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write from.</param>
        /// <param name="offset">Offset position in the buffer copy from.</param>
        /// <param name="count">Number of bytes to copy from the write_buffer.</param>
        private void WriteInternal(byte[] buffer, int offset, int count)
        {
            _bufferStream.Position = _writePosition;
            try
            {
                _bufferStream.Write(buffer, offset, count);
            }
            catch (Exception e)
            {
                throw new InvalidDataException("FrameBuilder was sent a frame larger than the session allows.", e);
            }

            _writePosition += count;

            // Update the stream length 
            _streamLength = _writePosition - _readPosition;
        }

        /// <summary>
        /// Moves the internal buffer stream from the read position to the end, to the beginning.
        /// Frees up space to write in the buffer.
        /// </summary>
        private void MoveStreamBytesToBeginning()
        {
            var i = 0;
            for (; i < _writePosition - _readPosition; i++)
            {
                _internalBuffer[i] = _internalBuffer[i + _readPosition];
            }

            // Update the length for the new size.
            _bufferStream.SetLength(i);
            //buffer_stream.Position -= write_position;

            // Reset the internal writer and reader positions.
            _streamLength = _writePosition = i;
            _readPosition = 0;
        }

        /// <summary>
        /// Writes the specified bytes to the FrameBuilder and parses them as they are copied.
        /// </summary>
        /// <param name="buffer">Byte buffer to write and parse.</param>
        /// <param name="offset">Offset in the byte buffer to copy from.</param>
        /// <param name="count">Number of bytes to write into the builder.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int maxWrite = count;
                // If we are over the byte limitation, then move the buffer back to the beginning of the stream and reset the stream.
                if (count + _writePosition > _internalBuffer.Length)
                {
                    MoveStreamBytesToBeginning();
                    maxWrite = Math.Min(Math.Abs(count - _writePosition), count);
                }


                WriteInternalPart(buffer, offset, maxWrite);
                offset += maxWrite;
                count -= maxWrite;
                //count 
            }
        }

        /// <summary>
        /// Writes the specified partial bytes to the FrameBuilder and parses them as they are copied.
        /// </summary>
        /// <param name="buffer">Byte buffer to write and parse.</param>
        /// <param name="offset">Offset in the byte buffer to copy from.</param>
        /// <param name="count">Number of bytes to write into the builder.</param>
        private void WriteInternalPart(byte[] buffer, int offset, int count)
        {
            // Write the incoming bytes to the stream.
            WriteInternal(buffer, offset, count);

            // Loop until we require more data
            while (true)
            {
                if (_currentFrameType == MqFrameType.Unset)
                {
                    var frameTypeBytes = new byte[1];

                    // This will always return one byte.
                    ReadInternal(frameTypeBytes, 0, 1);

                    if (frameTypeBytes[0] > _maxTypeEnum)
                    {
                        throw new InvalidDataException(
                            $"FrameBuilder was sent a frame with an invalid type.  Type sent: {frameTypeBytes[0]}");
                    }

                    _currentFrameType = (MqFrameType) frameTypeBytes[0];
                }

                if (_currentFrameType == MqFrameType.Empty ||
                    _currentFrameType == MqFrameType.EmptyLast ||
                    _currentFrameType == MqFrameType.Ping)
                {
                    EnqueueAndReset();
                    break;
                }

                // Read the length from the stream if there are enough buffer.
                if (_currentFrameData == null && _streamLength >= 2)
                {
                    var frameLen = new byte[2];

                    ReadInternal(frameLen, 0, frameLen.Length);
                    var currentFrameLength = BitConverter.ToUInt16(frameLen, 0);

                    if (currentFrameLength < 1)
                    {
                        throw new InvalidDataException(
                            $"FrameBuilder was sent a frame with an invalid size of {currentFrameLength}");
                    }

                    if (currentFrameLength > _internalBuffer.Length)
                    {
                        throw new InvalidDataException(
                            $"Frame size is {currentFrameLength} while the maximum size for frames is 16KB.");
                    }
                    _currentFrameData = new byte[currentFrameLength];

                    // Set the stream back to the position it was at to begin with.
                    //buffer_stream.Position = original_position;
                }

                // Read the data into the frame holder.
                if (_currentFrameData != null && _streamLength >= _currentFrameData.Length)
                {
                    ReadInternal(_currentFrameData, 0, _currentFrameData.Length);

                    // Create the frame and enqueue it.
                    EnqueueAndReset();

                    // If we are at the end of the data, complete this loop and wait for more data.
                    if (_writePosition == _readPosition)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Completes the current frame and adds it to the frame list.
        /// </summary>
        private void EnqueueAndReset()
        {
            Frames.Enqueue(new MqFrame(_currentFrameData, _currentFrameType, _config));
            _currentFrameType = MqFrameType.Unset;
            _currentFrameData = null;
        }

        /// <summary>
        /// Disposes of all resources held by the builder.
        /// </summary>
        public void Dispose()
        {
            _bufferStream.Dispose();

            // Delete all the Frames.
            var totalFrames = Frames.Count;
            for (int i = 0; i < totalFrames; i++)
            {
                var frame = Frames.Dequeue();

                frame.Dispose();
            }
        }
    }
}