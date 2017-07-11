using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public abstract class MqBaseTestSession<T> : MqSession<T, MqConfig> 
        where T : MqSession<T, MqConfig>, new()
    {
        protected MqMessageReader Reader;
        protected MqMessageWriter Writer;
        protected readonly Stopwatch Stopwatch = new Stopwatch();
        protected Timer ResponseTimer;

        public bool IsServer { get; set; }
        public bool CancelTest { get; set; }


        protected Random Rand = new Random();

        protected override void OnSetup()
        {
            base.OnSetup();

            Reader = new MqMessageReader();
            Writer = new MqMessageWriter(Config);
        }

        protected override void OnIncomingMessage(object sender, IncomingMessageEventArgs<T, MqConfig> e)
        {
            if (IsServer)
            {
                ServerMessage(e.Messages);
            }
            else
            {
                ClientMessage(e.Messages);
            }
        }




        protected byte[] RandomBytes(int len)
        {
            var val = new byte[len];
            Rand.NextBytes(val);
            return val;
        }



        public abstract void StartTest();
        protected abstract void ClientMessage(Queue<MqMessage> messageQueue);
        protected abstract void ServerMessage(Queue<MqMessage> messageQueue);
    }
}
