using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Mq
{
    [TestFixture]
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

        }

        [SetUp]
        public void Init()
        {
            _frameBuilder = new MqFrameBuilder(_config);
            _emptyLastFrame = new MqFrame(null, MqFrameType.EmptyLast, _config);
            _emptyFrame = new MqFrame(null, MqFrameType.Empty, _config);
            _lastFrame = new MqFrame(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, MqFrameType.Last, _config);
            _moreFrame = new MqFrame(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, MqFrameType.More, _config);
            _commandFrame = new MqFrame(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, MqFrameType.Command, _config);
            _pingFrame = new MqFrame(null, MqFrameType.Ping, _config);
        }

        [Test]
        public void FrameBuilder_parses_empty_frame()
        {
            _frameBuilder.Write(_emptyFrame.RawFrame());
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_emptyFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_parses_empty_last_frame()
        {
            _frameBuilder.Write(_emptyLastFrame.RawFrame());
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_emptyLastFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_parses_last_frame()
        {
            _frameBuilder.Write(_lastFrame.RawFrame());
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_lastFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_parses_more_frame()
        {
            _frameBuilder.Write(_moreFrame.RawFrame());
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_moreFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_parses_command_frame()
        {
            _frameBuilder.Write(_commandFrame.RawFrame());
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_commandFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_parses_ping_frame()
        {
            _frameBuilder.Write(_pingFrame.RawFrame());
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Utilities.CompareFrame(_pingFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_parses_multiple_frames()
        {
            _frameBuilder.Write(_moreFrame.RawFrame());
            _frameBuilder.Write(_moreFrame.RawFrame());

            while (_frameBuilder.Frames.Count > 0)
            {
                var parsedFrame = _frameBuilder.Frames.Dequeue();
                Utilities.CompareFrame(_moreFrame, parsedFrame);
            }
        }

        [Test]
        public void FrameBuilder_parses_frames_in_parts()
        {
            var frameBytes = _lastFrame.RawFrame();

            for (int i = 0; i < frameBytes.Length; i++)
            {
                _frameBuilder.Write(new[] {frameBytes[i]});
            }

            var parsedFrame = _frameBuilder.Frames.Dequeue();
            Utilities.CompareFrame(_lastFrame, parsedFrame);
        }

        [Test]
        public void FrameBuilder_throws_passed_buffer_too_large()
        {
            Assert.Throws<InvalidDataException>(
                () => { _frameBuilder.Write(new byte[_config.FrameBufferSize + 1]); });
        }

        [Test]
        public void FrameBuilder_throws_frame_zero_length()
        {
            Assert.Throws<InvalidDataException>(() => { _frameBuilder.Write(new byte[] {2, 0, 0, 1}); });
        }

        [Test]
        public void FrameBuilder_throws_frame_specified_length_too_large()
        {
            Assert.Throws<InvalidDataException>(() => { _frameBuilder.Write(new byte[] {2, 255, 255, 1}); });
        }

        [Test]
        public void FrameBuilder_throws_frame_type_out_of_range()
        {
            var maxTypeEnum = Enum.GetValues(typeof(MqFrameType)).Cast<byte>().Max() + 1;

            Assert.Throws<InvalidDataException>(() => { _frameBuilder.Write(new byte[] {(byte) maxTypeEnum}); });
        }

        [Test]
        public void FrameBuilder_parsed_frame_data()
        {
            _frameBuilder.Write(new byte[] {2, 10, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0});
            var parsedFrame = _frameBuilder.Frames.Dequeue();

            Assert.AreEqual(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, parsedFrame.Buffer);
        }

    }
}