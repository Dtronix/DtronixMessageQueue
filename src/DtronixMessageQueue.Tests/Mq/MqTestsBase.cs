using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.TransportLayer;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Mq
{
    public class MqTestsBase : TestBase
    {


        public MqServer<SimpleMqSession, MqConfig> Server { get; protected set; }
        public MqClient<SimpleMqSession, MqConfig> Client { get; protected set; }

        protected MqConfig ClientConfig;
        protected MqConfig ServerConfig;

        public override void Init()
        {
            base.Init();

            ClientConfig = new MqConfig
            {
                ConnectAddress = $"127.0.0.1:{Port}",
            };

            ServerConfig = new MqConfig
            {
                BindAddress = $"127.0.0.1:{Port}"
            };

            Server = new MqServer<SimpleMqSession, MqConfig>(ServerConfig);
            Client = CreateClient(ClientConfig);

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


        public bool CompareMessages(MqMessage expected, MqMessage actual)
        {
            try
            {

                // Total frame count comparison.
                Assert.AreEqual(expected.Count, actual.Count);

                for (int i = 0; i < expected.Count; i++)
                {
                    // Frame length comparison.
                    Assert.AreEqual(expected[i].DataLength, actual[i].DataLength);

                    Assert.AreEqual(expected[i].Buffer, actual[i].Buffer);
                }

                return true;
            }
            catch (Exception e)
            {
                LastException = e;
                return false;
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
                        (i + 1 < frameCount) ? MqFrameType.More : MqFrameType.Last, ClientConfig);
                }
                else
                {
                    frame = new MqFrame(Utilities.SequentialBytes(frameLength),
                        (i + 1 < frameCount) ? MqFrameType.More : MqFrameType.Last, ClientConfig);
                }
                message.Add(frame);
            }

            return message;
        }

    }
}