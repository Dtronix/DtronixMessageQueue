using System;
using System.Collections.Generic;
using System.Threading;

namespace DtronixMessageQueue.Tests.Gui.TestSessions
{
    public class MqThroughputTestSession : MqBaseTestSession
    {
        public long TotalThroughTime => _totalThroughTime;
        public int TotalThroughMessages => _totalThroughMessages;
        public int TotalThroughFrames => _totalThroughFrames;
        public int TotalThroughBytes => _totalThroughBytes;
        public DateTime StartedTime;

        private int _totalThroughBytes;
        private int _totalThroughFrames;
        private int _totalThroughMessages;
        private long _totalThroughTime;


        private int _configFrameSize;
        private int _configFramesPerMessage;


        public void ConfigTest(int frameSize, int framesPerMessage)
        {
            _configFrameSize = frameSize;
            _configFramesPerMessage = framesPerMessage;
        }

        public override void StartTest()
        {
            StartedTime = DateTime.Now;
            TestThread.Start(this);

            if (IsServer)
            {
                ResponseTimer = new Timer(ThroughputResponse);
                ResponseTimer.Change(1000, 1000);
                Stopwatch.Restart();
            }
        }

        protected override void ServerMessage(Queue<MqMessage> messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                Interlocked.Add(ref _totalThroughBytes, message.Size);
                _totalThroughMessages++;
                Interlocked.Add(ref _totalThroughFrames, message.Count);
            }

        }

        protected override void ClientMessage(Queue<MqMessage> messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                Reader.Message = message;
                var messageType = (ServerMessageType)Reader.ReadByte();

                if (messageType == ServerMessageType.ThroughputTransfer)
                {
                    _totalThroughBytes = Reader.ReadInt32();
                    _totalThroughMessages = Reader.ReadInt32();
                    _totalThroughFrames = Reader.ReadInt32();
                    _totalThroughTime = Reader.ReadInt64();
                }
            }
        }

        private void ThroughputResponse(object state)
        {
 
            using (var responseWriter = new MqMessageWriter(Config))
            {
                var throughBytes = TotalThroughBytes;
                var throughMessages = TotalThroughMessages;
                var throughFrames = TotalThroughFrames;

                _totalThroughBytes = 0;
                _totalThroughMessages = 0;
                _totalThroughFrames = 0;

                responseWriter.Write((byte)ServerMessageType.ThroughputTransfer);
                responseWriter.Write(throughBytes);
                responseWriter.Write(throughMessages);
                responseWriter.Write(throughFrames);
                responseWriter.Write(Stopwatch.ElapsedMilliseconds);

                Stopwatch.Restart();

                Send(responseWriter.ToMessage(true));
            }
        }

        protected override void TestThreadAction(object state)
        {
            var session = (MqThroughputTestSession) state;
            var message = new MqMessage();
            var frame = new MqFrame(RandomBytes(session._configFrameSize), session.Config);

            for (int i = 0; i < session._configFramesPerMessage; i++)
            {
                message.Add(frame);
            }


            // Send messages until it is called to stop.
            while (!session.CancelTest)
            {
                session.Send(message);
            }

        }

        public override void Close(SocketCloseReason reason)
        {
            if (ResponseTimer != null)
            {
                ResponseTimer.Change(-1, -1);
                ResponseTimer.Dispose();
            }
            base.Close(reason);

        }
    }

}