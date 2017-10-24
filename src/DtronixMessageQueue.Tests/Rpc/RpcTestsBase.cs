using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.TransportLayer;
using NUnit.Framework;

namespace DtronixMessageQueue.Tests.Rpc
{
    public class RpcTestsBase : TestBase
    {

        public RpcServer<SimpleRpcSession, RpcConfig> Server { get; protected set; }
        public RpcClient<SimpleRpcSession, RpcConfig> Client { get; protected set; }

        protected RpcConfig ClientConfig;
        protected RpcConfig ServerConfig;


        public override void Init()
        {
            base.Init();

            ClientConfig = new RpcConfig
            {
                Address = $"127.0.0.1:{Port}",
            };

            ServerConfig = new RpcConfig
            {
                Address = $"127.0.0.1:{Port}",
            };

            Server = new RpcServer<SimpleRpcSession, RpcConfig>(ServerConfig);
            Client = new RpcClient<SimpleRpcSession, RpcConfig>(ClientConfig);
        }




        protected override void StopClientServer()
        {
            try
            {
                Server.Stop();
            }
            catch
            {
            }
            try
            {
                Client.Close();
            }
            catch
            {
            }
        }


        public void StartAndWait(bool timeoutError = true, int timeoutLength = -1, bool startServer = true, bool startClient = true)
        {

            if (startServer)
                Server.Start();

            if (startClient)
                Client.Connect();

            base.StartAndWait(timeoutError, timeoutLength);
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