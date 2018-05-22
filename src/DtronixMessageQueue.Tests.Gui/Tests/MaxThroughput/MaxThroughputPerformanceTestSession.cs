using System;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Gui.Tests.MaxThroughput
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
            Task.Run(() =>
            {
                StartedTime = DateTime.Now;

                while (RunTest && CurrentState == State.Connected)
                {
                    Send(_testMessage);
                }
            });
        }


        public override void Close(CloseReason reason)
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