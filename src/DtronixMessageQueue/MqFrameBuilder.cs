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
        /// Data for this frame
        /// </summary>
        private byte[] _currentFrameData;

        /// <summary>
        /// Current parsed type.  is Unknown by default.
        /// </summary>
        private MqFrameType _currentFrameType;

        /// <summary>
        /// Memory stream used to read and write to the internal buffer.
        /// </summary>
        private MemoryQueueBufferStream _bufferStream;

        /// <summary>
        /// Used to cache the maximum size of the MqFrameType.
        /// </summary>
        private static int _maxTypeEnum = -1;

        /// <summary>
        /// Configurations for the connected session.
        /// </summary>
        private readonly MqConfig _config;

        public Queue<MqFrame> Frames { get; }

        /// <summary>
        /// Size in bytes of the header of a frame.
        /// MqFrameType:byte[1], Length:UInt16[2]
        /// </summary>
        public const int HeaderLength = 3;


        /// <summary>
        /// Creates a new instance of the frame builder to handle parsing of incoming byte stream.
        /// </summary>
        /// <param name="config">Socket configurations for this session.</param>
        public MqFrameBuilder(MqConfig config)
        {
            _config = config;
            Frames = new Queue<MqFrame>();
            _bufferStream = new MemoryQueueBufferStream();

            // Determine what our max enum value is for the FrameType
            if (_maxTypeEnum == -1)
            {
                _maxTypeEnum = Enum.GetValues(typeof(MqFrameType)).Cast<byte>().Max();
            }
        }

        /// <summary>
        /// Writes the specified bytes to the FrameBuilder and parses them as they are read.
        /// </summary>
        /// <param name="buffer">Byte buffer to write and parse.</param>
        public void Write(byte[] buffer)
        {
            _bufferStream.Write(buffer);

            // Loop until we require more data
            while (true)
            {
                if (_currentFrameType == MqFrameType.Unset)
                {
                    var frameTypeBytes = new byte[1];

                    // This will always return one byte.
                    _bufferStream.Read(frameTypeBytes, 0, 1);

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
                    continue;
                }

                // Read the length from the stream if there are enough buffer.
                if (_currentFrameData == null && _bufferStream.Length >= 2)
                {
                    var frameLen = new byte[2];

                    _bufferStream.Read(frameLen, 0, frameLen.Length);
                    var currentFrameLength = BitConverter.ToUInt16(frameLen, 0);

                    if (currentFrameLength < 1)
                    {
                        throw new InvalidDataException(
                            $"FrameBuilder was sent a frame with an invalid size of {currentFrameLength}");
                    }

                    if (currentFrameLength > _bufferStream.Length)
                    {
                        throw new InvalidDataException(
                            $"Frame size is {currentFrameLength} while the maximum size for frames is 16KB.");
                    }
                    _currentFrameData = new byte[currentFrameLength];

                    // Set the stream back to the position it was at to begin with.
                    //buffer_stream.Position = original_position;
                }

                // Read the data into the frame holder.
                if (_currentFrameData != null && _bufferStream.Length >= _currentFrameData.Length)
                {
                    _bufferStream.Read(_currentFrameData, 0, _currentFrameData.Length);

                    // Create the frame and enqueue it.
                    EnqueueAndReset();

                    // If we are at the end of the data, complete this loop and wait for more data.
                    if (_bufferStream.Length == 0)
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