using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.TransportLayer;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqTestsBase : TestBase
    {


        public MqServer<SimpleMqSession, MqConfig> Server { get; protected set; }
        public MqClient<SimpleMqSession, MqConfig> Client { get; protected set; }

        protected MqConfig Config;


        public MqTestsBase(ITestOutputHelper output) : base(output)
        {
            Config = new MqConfig
            {
                ConnectAddress = $"127.0.0.1:{Port}",
                BindAddress = $"127.0.0.1:{Port}"
                
            };

            Server = new MqServer<SimpleMqSession, MqConfig>(Config);
            Client = CreateClient(Config);
        }


        public void StartAndWait(bool timeoutError = true, int timeoutLength = -1, bool startServer = true, bool startClient = true)
        {
            if (startServer)
                Server.Start();

            if (startClient)
                Client.Connect();

            base.StartAndWait(timeoutError, timeoutLength);
        }

        protected override void StopClientServer()
        {
            try
            {
                Server.Stop();
            }
            catch
            {
                // ignored
            }
            try
            {
                Client.Close();
            }
            catch
            {
                // ignored
            }
        }


        public MqClient<SimpleMqSession, MqConfig> CreateClient(MqConfig config)
        {
            return new MqClient<SimpleMqSession, MqConfig>(config);
        }


        public void CompareMessages(MqMessage expected, MqMessage actual)
        {
            try
            {
                // Total frame count comparison.
                Assert.Equal(expected.Count, actual.Count);

                for (int i = 0; i < expected.Count; i++)
                {
                    // Frame length comparison.
                    Assert.Equal(expected[i].DataLength, actual[i].DataLength);

                    Assert.Equal(expected[i].Buffer, actual[i].Buffer);
                }
            }
            catch (Exception e)
            {
                LastException = e;
            }
        }

        public MqMessage GenerateRandomMessage(int frames = -1, int frameLength = -1)
        {
            var frameCount = frames == -1 ? Random.Next(8, 16) : frames;
            var message = new MqMessage();
            for (int i = 0; i < frameCount; i++)
            {
                MqFrame frame;

                if (frameLength == -1)
                {
                    frame = new MqFrame(Utilities.SequentialBytes(Random.Next(50, 1024 * 16 - 3)),
                        (i + 1 < frameCount) ? MqFrameType.More : MqFrameType.Last, Config);
                }
                else
                {
                    frame = new MqFrame(Utilities.SequentialBytes(frameLength),
                        (i + 1 < frameCount) ? MqFrameType.More : MqFrameType.Last, Config);
                }
                message.Add(frame);
            }

            return message;
        }

    }
}