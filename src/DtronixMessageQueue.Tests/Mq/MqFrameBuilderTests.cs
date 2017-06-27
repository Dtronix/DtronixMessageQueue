using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqFrameBuilderTests
    {
        private MqFrame _emptyLastFrame;
        private MqFrame _emptyFrame;
        private MqFrame _lastFrame;
        private MqFrame _moreFrame;
        private MqFrameBuilder _frameBuilder;
        private MqConfig _config = new MqConfig();
        private MqFrame _commandFrame;
        private MqFrame _pingFrame;

        public MqFrameBuilderTests()
        {
            _frameBuilder = new MqFrameBuilder(_config);
            _emptyLastFrame = new MqFrame(null, MqFrameType.EmptyLast, _config);
            _emptyFrame = new MqFrame(null, MqFrameType.Empty, _config);
            _lastFrame = new MqFrame(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, MqFrameType.Last, _config);
            _moreFrame = new MqFrame(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, MqFrameType.More, _config);
            _commandFrame = new MqFrame(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, MqFrameType.Command, _config);
            _pingFrame = new MqFrame(null, MqFrameType.Ping, _config);
        }

        [Fact]
        public void FrameBuilder_parses_empty_frame()
        {
            _frameBuilder.Write(_emptyFrame.RawFrame(), 0, _emptyFrame.FrameSize);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_emptyFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_parses_empty_last_frame()
        {
            _frameBuilder.Write(_emptyLastFrame.RawFrame(), 0, _emptyFrame.FrameSize);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_emptyLastFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_parses_last_frame()
        {
            _frameBuilder.Write(_lastFrame.RawFrame(), 0, _lastFrame.FrameSize);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_lastFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_parses_more_frame()
        {
            _frameBuilder.Write(_moreFrame.RawFrame(), 0, _moreFrame.FrameSize);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_moreFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_parses_command_frame()
        {
            _frameBuilder.Write(_commandFrame.RawFrame(), 0, _commandFrame.FrameSize);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_commandFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_parses_ping_frame()
        {
            _frameBuilder.Write(_pingFrame.RawFrame(), 0, _pingFrame.FrameSize);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_pingFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_parses_multiple_frames()
        {
            _frameBuilder.Write(_moreFrame.RawFrame(), 0, _moreFrame.FrameSize);
            _frameBuilder.Write(_moreFrame.RawFrame(), 0, _moreFrame.FrameSize);

            while (_frameBuilder.Frames.Count > 0)
            {
                var parsedFrame = _frameBuilder.Frames.Dequeue();
                Utilities.CompareFrame(_moreFrame, parsedFrame);
            }
        }

        [Fact]
        public void FrameBuilder_parses_frames_in_parts()
        {
            var frameBytes = _lastFrame.RawFrame();

            for (int i = 0; i < frameBytes.Length; i++)
            {
                _frameBuilder.Write(new[] {frameBytes[i]}, 0, 1);
            }

            var parsedFrame = _frameBuilder.Frames.Dequeue();
            Utilities.CompareFrame(_lastFrame, parsedFrame);
        }

        [Fact]
        public void FrameBuilder_throws_passed_buffer_too_large()
        {
            Assert.Throws<InvalidDataException>(
                () => { _frameBuilder.Write(new byte[_config.FrameBufferSize + 1], 0, _config.FrameBufferSize + 1); });
        }

        [Fact]
        public void FrameBuilder_throws_frame_zero_length()
        {
            Assert.Throws<InvalidDataException>(() => { _frameBuilder.Write(new byte[] {2, 0, 0, 1}, 0, 4); });
        }

        [Fact]
        public void FrameBuilder_throws_frame_specified_length_too_large()
        {
            Assert.Throws<InvalidDataException>(() => { _frameBuilder.Write(new byte[] {2, 255, 255, 1}, 0, 4); });
        }

        [Fact]
        public void FrameBuilder_throws_frame_type_out_of_range()
        {
            var maxTypeEnum = Enum.GetValues(typeof(MqFrameType)).Cast<byte>().Max() + 1;

            Assert.Throws<InvalidDataException>(() => { _frameBuilder.Write(new byte[] {(byte) maxTypeEnum}, 0, 1); });
        }

        [Fact]
        public void FrameBuilder_parsed_frame_data()
        {
            _frameBuilder.Write(new byte[] {2, 10, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, 0, 13);
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, parsedFrame.Buffer);
        }
    }
}