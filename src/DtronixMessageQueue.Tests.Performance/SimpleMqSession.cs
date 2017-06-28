using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Tests.Performance
{
    public class SimpleMqSession : MqSession<SimpleMqSession, MqConfig>
    {
        private MqMessageReader reader;
        private MqMessageWriter writer;

        public bool IsServer { get; set; }

        public TestMode Mode { get; set; }

        private Stopwatch stopwatch = new Stopwatch();

        private Timer _responseTimer;
        private int _totalThroughBytes;
        private int _totalThroughFrames;
        private int _totalThroughMessages;

        protected override void OnSetup()
        {
            base.OnSetup();

            reader = new MqMessageReader();
            writer = new MqMessageWriter(Config);
        }

        protected override void OnIncomingMessage(object sender, IncomingMessageEventArgs<SimpleMqSession, MqConfig> e)
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

        private void ClientMessage(Queue<MqMessage> message_queue)
        {
            
        }

        public override void Close(SocketCloseReason reason)
        {
            if (_responseTimer != null)
            {
                _responseTimer.Change(-1, -1);
                _responseTimer.Dispose();
            }
            base.Close(reason);

        }

        private void ServerMessage(Queue<MqMessage> message_queue)
        {
            
            if (Mode == TestMode.Unset)
            {
                reader.Message = message_queue.Dequeue();

                Mode = (TestMode)reader.ReadByte(); // Mode

                if (Mode == TestMode.Throughput)
                {
                    _responseTimer = new Timer(ThroughputResponse);
                    writer.Write((byte)ServerMessageType.Ready);
                    Send(writer.ToMessage(true));

                    _responseTimer.Change(1000, 1000);
                }
                
            }

            while (message_queue.Count > 0)
            {
                var message = message_queue.Dequeue();
                if (Mode == TestMode.Throughput)
                {
                    Interlocked.Add(ref _totalThroughBytes, message.Size);
                    _totalThroughMessages++;
                    Interlocked.Add(ref _totalThroughFrames, message.Count);
                }

            }

        }

        private void ThroughputResponse(object state)
        {
            using (var writer = new MqMessageWriter(Config))
            {
                var throughBytes = _totalThroughBytes;
                var throughMessages = _totalThroughMessages;
                var throughFrames = _totalThroughFrames;

                _totalThroughBytes = 0;
                _totalThroughMessages = 0;
                _totalThroughFrames = 0;

                writer.Write((byte)ServerMessageType.ThroughputTransfer);
                writer.Write(throughBytes);
                writer.Write(throughMessages);
                writer.Write(throughFrames);
                writer.Write(stopwatch.ElapsedMilliseconds);
            }
        }

        
    }

    public enum TestMode
    {
        Unset = 0,
        Throughput = 1,
        Repeat = 2
    }

    public enum ServerMessageType : byte
    {
        Unset = 0,
        Ready = 1,
        Complete = 2,
        ThroughputTransfer = 3
    }
}