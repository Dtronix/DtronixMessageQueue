using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue.TcpSocket;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public abstract class MqBaseTestSession<T> : MqSession<T, MqConfig> 
        where T : MqSession<T, MqConfig>, new()
    {

        public static long TotalReceieved;
        public static long TotalSent;
        protected bool RunTest = true;

        protected MqMessageReader Reader;
        protected MqMessageWriter Writer;
        protected readonly Stopwatch Stopwatch = new Stopwatch();
        protected Timer ResponseTimer;

        public bool IsServer { get; set; }
        public bool CancelTest { get; set; }





        protected Random Rand = new Random();

        public long GetTotalReceived()
        {
            return TotalReceieved;
        }

        public long GetTotalSent()
        {
            return TotalSent;
        }

        protected override void OnSetup()
        {
            base.OnSetup();

            Reader = new MqMessageReader();
            Writer = new MqMessageWriter(Config);

            IsServer = SocketHandler.Mode == TcpSocketMode.Server;
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

        protected override void Send(byte[] buffer, int offset, int count)
        {
            base.Send(buffer, offset, count);
            Interlocked.Add(ref TotalSent, count);
        }


        public abstract void StartTest();


        protected virtual void ServerMessage(Queue<MqMessage> messageQueue)
        {
            for (int i = 0; i < messageQueue.Count; i++)
            {
                var message = messageQueue.Dequeue();
                Interlocked.Add(ref TotalReceieved, message.Size);
            }
        }

        protected virtual void ClientMessage(Queue<MqMessage> messageQueue)
        {
            for (int i = 0; i < messageQueue.Count; i++)
            {
                var message = messageQueue.Dequeue();
                Interlocked.Add(ref TotalReceieved, message.Size);
            }
        }

        public virtual void PauseTest()
        {
            if (RunTest)
            {
                RunTest = false;
            }
            else
            {
                RunTest = true;
                StartTest();
            }

        }
    }
}
