using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class MaxThroughputPerformanceTestSession : MqBaseTestSession<MaxThroughputPerformanceTestSession>
    {
        public DateTime StartedTime;

        private int _configFrameSize;
        private MqMessage _testMessage;

        public void ConfigTest(int frameSize, int totalFrames)
        {
            _configFrameSize = frameSize;
            var writer = new MqMessageWriter(Config);


            for (int i = 0; i < totalFrames; i++)
            {
                writer.Write(RandomBytes(_configFrameSize));
            }

            _testMessage = writer.ToMessage(true);
        }

        public override void StartTest()
        {
            StartedTime = DateTime.Now;

            var task = new Task( async () =>
            {
                while (CurrentState == State.Connected)
                {
                    Send(_testMessage);
                    await Task.Delay(5);
                }
            }, TaskCreationOptions.LongRunning);

            task.Start();
        }


        public override void Close(SocketCloseReason reason)
        {
            if (ResponseTimer != null)
            {
                ResponseTimer.Change(-1, -1);
                ResponseTimer.Dispose();
                ResponseTimer = null;
            }
            base.Close(reason);

        }
    }

}