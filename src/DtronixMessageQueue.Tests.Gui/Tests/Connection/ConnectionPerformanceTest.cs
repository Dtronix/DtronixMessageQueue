using System.Collections.Generic;
using System.Threading.Tasks;
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
            Control = ActualControl = new ConnectionPerformanceTestControl(this);
            _testClients = new List<MqClient<ConnectionPerformanceTestSession, MqConfig>>();
        }



        public override void StartServer()
        {

            TestController.Log("Starting Test ControlServer");

            if (_testServer == null)
            {
                _testServer = new MqServer<ConnectionPerformanceTestSession, MqConfig>(new MqConfig
                {
                    ConnectAddress = "0.0.0.0",
                    Port = 2121,
                    PingTimeout = 1000,
                    MaxConnections = 2000

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
            var configClients = ActualControl.ConfigClients;
            var configPackageLength = ActualControl.ConfigBytesPerMessage;
            var configPeriod = ActualControl.ConfigMessagePeriod;

            Task.Run(() =>
            {
                for (int i = 0; i < configClients; i++)
                {
                    var client = new MqClient<ConnectionPerformanceTestSession, MqConfig>(new MqConfig
                    {
                        ConnectAddress = TestController.ControllClient.Config.ConnectAddress,
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
            });
        }

        public override void TestControllerStartTest(ControllerSession session)
        {
            TestController.MainWindow.Dispatcher.Invoke(() =>
            {
                session.GetProxy<IControllerService>()
                    .StartConnectionTest(ActualControl.ConfigClients,
                        ActualControl.ConfigBytesPerMessage,
                        ActualControl.ConfigMessagePeriod);
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
