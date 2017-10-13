using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DtronixMessageQueue.Tests.Mq;
using DtronixMessageQueue.TransportLayer;
using DtronixMessageQueue.TransportLayer.Tcp;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.TransportLayer
{
    public abstract class TransportLayerTestsBase : TestBase
    {

        public ITransportLayer Client { get; }
        public ITransportLayer Server { get; }
        public TransportLayerConfig Config { get; set; }

        protected TransportLayerTestsBase(ITestOutputHelper output) : base(output)
        {
            Config = new MqConfig
            {
                ConnectAddress = $"127.0.0.1:{Port}",
                BindAddress = $"127.0.0.1:{Port}"
            };

            Server = new TcpTransportLayer(Config, TransportLayerMode.Server);
            Client = new TcpTransportLayer(Config, TransportLayerMode.Client);
        }

        public void StartAndWait(bool timeoutError = true, int timeoutLength = -1, bool startServer = true, bool startClient = true)
        {
            if(startServer)
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
                Client.Close(SessionCloseReason.Closing);
            }
            catch
            {
                // ignored
            }
        }
    }
}