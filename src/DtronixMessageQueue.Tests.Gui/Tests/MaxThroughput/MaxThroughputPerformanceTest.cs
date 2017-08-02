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


        public MaxThroughputPerformanceTest(TestController testController) : base("Max Throughput Test", testController)
        {
            Control = ActualControl = new MaxThroughputPerformanceTestControl(this);
            _testClients = new List<MqClient<MaxThroughputPerformanceTestSession, MqConfig>>();
        }


        public override void StartServer()
        {

            TestController.Log("Starting Test ControlServer");

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
                    ConnectionAdded();
                };

                _testServer.Closed += (sender, args) =>
                {
                    ConnectionRemoved(args.CloseReason);
                };
            }

            _testServer.Start();
        }

        public override void StartClient()
        {
            var configClientConnections = ActualControl.ConfigClients;
            var configFrames = ActualControl.ConfigFrames;
            var configFrameSize = ActualControl.ConfigFrameSize;

            for (int i = 0; i < configClientConnections; i++)
            {
                var client = new MqClient<MaxThroughputPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Ip = TestController.ControllClient.Config.Ip,
                    Port = 2121,
                    PingFrequency = 500
                });

                client.Connected += (sender, args) =>
                {
                    ConnectionAdded();
                    args.Session.ConfigTest(configFrameSize, configFrames);
                    args.Session.StartTest();
                };

                client.Closed += (sender, args) =>
                {
                    ConnectionRemoved(args.CloseReason);
                };

                client.Connect();

                _testClients.Add(client);
            }
        }

        public override void TestControllerStartTest(ControllerSession session)
        {
            TestController.MainWindow.Dispatcher.Invoke(() =>
            {
                session.GetProxy<IControllerService>()
                    .StartMaxThroughputTest(ActualControl.ConfigClients, ActualControl.ConfigFrames,
                        ActualControl.ConfigFrameSize);
            });
        }

        public override void PauseResumeTest()
        {
            if (_testClients.Count > 0)
                foreach (var client in _testClients)
                    client.Session.PauseTest();
        }

        public override void StopTest()
        {
            if (_testClients.Count > 0)
            {
                foreach (var client in _testClients)
                    client.Close();

                _testClients.Clear();
            }

            _testServer?.Stop();
            _testServer = null;
        }


    }
    
}
