using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;


namespace DtronixMessageQueue.Tests.TransportLayer
{
    public abstract class TransportLayerTestsBase : TestBase
    {

        public ITransportLayer Client { get; set; }
        public ITransportLayer Server { get; set; }

        public TransportLayerConfig ClientConfig { get; set; }
        public TransportLayerConfig ServerConfig { get; set; }


        public override void Init()
        {
            base.Init();

            ClientConfig = new TransportLayerConfig
            {
                ConnectAddress = $"127.0.0.1:{Port}",
            };

            ServerConfig = new TransportLayerConfig
            {
                BindAddress = $"127.0.0.1:{Port}"
            };

            Server = new TcpTransportLayer(ServerConfig, TransportLayerMode.Server);
            Client = new TcpTransportLayer(ClientConfig, TransportLayerMode.Client);
        }


        public void StartAndWait(bool timeoutError = true, int timeoutLength = -1, bool startServer = true, bool startClient = true)
        {
            if (startServer)
                Server.Start();

            if(startClient)
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
                Client.ClientSession.Close();
            }
            catch
            {
                // ignored
            }
        }
    }
}