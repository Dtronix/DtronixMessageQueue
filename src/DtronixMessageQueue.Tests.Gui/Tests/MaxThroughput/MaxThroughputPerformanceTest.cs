using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests.MaxThroughput
{
    public class MaxThroughputPerformanceTest : PerformanceTest
    {

        private MqServer<MaxThroughputPerformanceTestSession, MqConfig> _testServer;

        private List<MqClient<MaxThroughputPerformanceTestSession, MqConfig>> _testClients;


        public MaxThroughputPerformanceTestControl ActualControl { get; }


        public MaxThroughputPerformanceTest(MainWindow mainWindow) : base("Max Throughput Test", mainWindow)
        {
            Control = ActualControl = new MaxThroughputPerformanceTestControl();
            _testClients = new List<MqClient<MaxThroughputPerformanceTestSession, MqConfig>>();
        }


        public override void StartServer()
        {

            Log("Starting Test ControlServer");

            if (_testServer == null)
            {
                _testServer = new MqServer<MaxThroughputPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Ip = "0.0.0.0",
                    Port = 2121,
                    PingTimeout = 8000,
                    MaxConnections = 1000

                });

                _testServer.Connected += (sender, args) =>
                {
                    Log("New Test ControllClient Connected");
                    Interlocked.Increment(ref TotalConnections);
                    Update();
                };

                _testServer.Closed += (sender, args) =>
                {
                    Log($"Test ControllClient Disconnected. Reason: {args.CloseReason}");
                    Interlocked.Decrement(ref TotalConnections);
                    Update();
                };
            }

            _testServer.Start();

            StartControlServer();
        }

        public override void StartClient()
        {


            var configClientConnections = ActualControl.ConfigClients;
            var configFrames = ActualControl.ConfigFrames;
            var configFrameSize = ActualControl.ConfigFrameSize;
            var ip = MainWindow.IpAddress;

            for (int i = 0; i < configClientConnections; i++)
            {
                var client = new MqClient<MaxThroughputPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Ip = ip,
                    Port = 2121,
                    PingFrequency = 500
                });

                client.Connected += (sender, args) =>
                {
                    args.Session.ConfigTest(configFrameSize, configFrames);
                    Log("Connection test client connected.");
                    Interlocked.Increment(ref TotalConnections);
                    Update();

                    args.Session.StartTest();
                };

                client.Closed += (sender, args) =>
                {
                    Interlocked.Decrement(ref TotalConnections);
                    Update();
                };

                Log("Connection test client connecting...");
                client.Connect();

                _testClients.Add(client);
            }
        }

        public override void PauseTest()
        {
            if (ControlServer != null)
                base.PauseTest();
            
            if(_testClients.Count > 0)
                foreach (var client in _testClients)
                    client.Session.PauseTest();

        }


        public void Update()
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                ActualControl.TotalConnections = TotalConnections;
            });
        }

        protected override void OnServerReady(SessionEventArgs<ControllerSession, RpcConfig> args)
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                args.Session.GetProxy<IControllerService>()
                    .StartMaxThroughputTest(ActualControl.ConfigClients, ActualControl.ConfigFrames,
                        ActualControl.ConfigFrameSize);
            });
        }

        public override void StopTest()
        {
            base.StopTest();
            _testServer?.Stop();
        }


    }
    
}
