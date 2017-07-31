using System.Collections.Generic;
using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests.Connection
{
    public class ConnectionPerformanceTest : PerformanceTest
    {

        private MqServer<ConnectionPerformanceTestSession, MqConfig> _testServer;
        private List<MqClient<ConnectionPerformanceTestSession, MqConfig>> _testClients;

        public ConnectionPerformanceTestControl ActualControl;


        public ConnectionPerformanceTest(TestController testController) : base("Connection Test", testController)
        {
            Control = ActualControl = new ConnectionPerformanceTestControl();
            _testClients = new List<MqClient<ConnectionPerformanceTestSession, MqConfig>>();
        }



        public override void StartServer()
        {

            TestController.Log("Starting Test ControlServer");

            if (_testServer == null)
            {
                _testServer = new MqServer<ConnectionPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Ip = "0.0.0.0",
                    Port = 2121,
                    PingTimeout = 1000,
                    MaxConnections = 200

                });

                _testServer.Connected += (sender, args) =>
                {
                    TestController.Log("New Test ControllClient Connected");
                };

                _testServer.Closed += (sender, args) =>
                {
                    TestController.Log($"Test ControllClient Disconnected. Reason: {args.CloseReason}");
                };
            }

            _testServer.Start();
        }

        public override void StartClient()
        {
            var configClients = ActualControl.ConfigClients;
            var configPackageLength = ActualControl.ConfigBytesPerMessage;
            var configPeriod = ActualControl.ConfigMessagePeriod;

            for (int i = 0; i < configClients; i++)
            {
                var client = new MqClient<ConnectionPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Ip = ControllClient.Config.Ip,
                    Port = 2121,
                    PingFrequency = 500
                });

                client.Connected += (sender, args) =>
                {
                    ConnectionAdded();
                    args.Session.ConfigTest(configPackageLength, configPeriod);
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


        protected override void OnServerReady(SessionEventArgs<ControllerSession, RpcConfig> args)
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                args.Session.GetProxy<IControllerService>()
                    .StartConnectionTest(ActualControl.ConfigClients, ActualControl.ConfigBytesPerMessage,
                        ActualControl.ConfigMessagePeriod);
            });
        }

        public override void PauseTest()
        {
            TestController.Pause();
                base.PauseTest();
        }



        public override void StopTest()
        {
            base.StopTest();
            _testServer?.Stop();
        }

        protected override void Update()
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                ActualControl.TotalConnections = TotalConnections;
            });
        }
    }

}
