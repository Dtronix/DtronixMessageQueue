using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests.Echo
{
    public class EchoPerformanceTest : PerformanceTest
    {

        private MqServer<EchoPerformanceTestSession, MqConfig> _testServer;

        private List<MqClient<EchoPerformanceTestSession, MqConfig>> _testClients;


        public EchoPerformanceTestControl ActualControl { get; }


        public EchoPerformanceTest(TestController testController) : base("Echo Performance Test", testController)
        {
            Control = ActualControl = new EchoPerformanceTestControl(this);
            _testClients = new List<MqClient<EchoPerformanceTestSession, MqConfig>>();
        }


        public override void StartServer()
        {
            TestController.Log("Starting Test ControlServer");

            if (_testServer == null)
            {
                _testServer = new MqServer<EchoPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Address = "0.0.0.0:2121",
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
            var configFrameSize = ActualControl.ConfigFrameSize;
            var addressParts = TestController.ControllClient.Config.Address.Split(':');

            for (int i = 0; i < configClientConnections; i++)
            {
                var client = new MqClient<EchoPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Address = addressParts[0] + ":2121",
                    PingFrequency = 500
                });

                client.Connected += (sender, args) =>
                {
                    ConnectionAdded();
                    args.Session.ConfigTest(configFrameSize);
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
                    .StartEchoTest(ActualControl.ConfigClients, ActualControl.ConfigFrameSize);
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
