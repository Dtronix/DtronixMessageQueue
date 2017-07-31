using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests.Connection
{
    public class ConnectionPerformanceTest : PerformanceTest
    {

        private MqServer<ConnectionPerformanceTestSession, MqConfig> _testServer;

        public ConnectionPerformanceTestControl ActualControl;


        public ConnectionPerformanceTest(MainWindow mainWindow) : base("Connection Test", mainWindow)
        {
            Control = ActualControl = new ConnectionPerformanceTestControl();
        }



        public override void StartServer()
        {

            Log("Starting Test ControlServer");

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
                    Log("New Test ControllClient Connected");
                };

                _testServer.Closed += (sender, args) =>
                {
                    Log($"Test ControllClient Disconnected. Reason: {args.CloseReason}");
                };
            }

            _testServer.Start();
        }

        public override void StartClient()
        {
            throw new System.NotImplementedException();
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
            if (ControlServer != null)
                base.PauseTest();

            /*if (_testClients.Count > 0)
                foreach (var client in _testClients)
                    client.Session.PauseTest();*/

        }



        public override void StopTest()
        {
            base.StopTest();
            _testServer?.Stop();
        }



    }

}
