using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue.Tests.Gui.Tests.Echo
{
    public class EchoPerformanceTestSession : MqBaseTestSession<EchoPerformanceTestSession>
    {
        public DateTime StartedTime;

        private int _configFrameSize;
        private MqMessage _testMessage;
        public static int MessageCount;
        
        public void ConfigTest(int frameSize)
        {
            _configFrameSize = frameSize;
            var writer = new MqMessageWriter(Config);
            writer.Write(RandomBytes(_configFrameSize));

            _testMessage = writer.ToMessage(true);
        }

        protected override void ServerMessage(Queue<MqMessage> messageQueue)
        {
            
            var message = messageQueue.Dequeue();
            if (RunTest)
                Send(message);
            messageQueue.Enqueue(message);

            Interlocked.Increment(ref MessageCount);

            base.ClientMessage(messageQueue);


        }

        protected override void ClientMessage(Queue<MqMessage> messageQueue)
        {
            var message = messageQueue.Dequeue();

            if(RunTest)
                Send(message);
            messageQueue.Enqueue(message);
            Interlocked.Increment(ref MessageCount);
            base.ClientMessage(messageQueue);



        }

        public override void StartTest()
        {
            Send(_testMessage);
        }


        public override void Close(SessionCloseReason reason)
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