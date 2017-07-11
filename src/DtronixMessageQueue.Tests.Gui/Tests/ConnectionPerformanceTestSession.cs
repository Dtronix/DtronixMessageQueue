using System;
using System.Collections.Generic;
using System.Threading;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class ConnectionPerformanceTestSession : MqBaseTestSession<ConnectionPerformanceTestSession>
    {
        public DateTime StartedTime;

        private int _configFrameSize;
        private MqMessage _testMessage;


        public void ConfigTest(int frameSize)
        {
            _configFrameSize = frameSize;

            var testFrame = CreateFrame(RandomBytes(_configFrameSize));
            _testMessage = new MqMessage(testFrame);
            

        }

        public override void StartTest()
        {
            StartedTime = DateTime.Now;

            if (!IsServer)
            {
                ResponseTimer = new Timer(RandomByteMessage);
                ResponseTimer.Change(1000, 1000);
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