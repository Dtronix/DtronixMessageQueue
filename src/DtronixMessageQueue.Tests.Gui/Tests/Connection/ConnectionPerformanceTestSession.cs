using System;
using System.Threading;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Tests.Gui.Tests.Connection
{
    public class ConnectionPerformanceTestSession : MqBaseTestSession<ConnectionPerformanceTestSession>
    {
        public DateTime StartedTime;

        private int _configFrameSize;
        private int _period;
        private MqMessage _testMessage;


        public void ConfigTest(int frameSize, int period)
        {
            _configFrameSize = frameSize;
            _period = period;
            var testFrame = CreateFrame(RandomBytes(_configFrameSize));
            _testMessage = new MqMessage(testFrame);
            

        }

        public override void StartTest()
        {
            StartedTime = DateTime.Now;

            if (!IsServer)
            {
                ResponseTimer = new Timer(RandomByteMessage);
                ResponseTimer.Change(_period, _period);
                Stopwatch.Restart();
            }
        }


        private void RandomByteMessage(object state)
        {
            Send(_testMessage);
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