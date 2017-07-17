using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests.Connection
{
    public class ConnectionPerformanceTest : PerformanceTest
    {

        private MqServer<ConnectionPerformanceTestSession, MqConfig> _testServer;

        private ConnectionPerformanceTestControl _control;


        public ConnectionPerformanceTest(MainWindow mainWindow) : base("Connection Test", mainWindow)
        {
            _control = new ConnectionPerformanceTestControl();
        }

        public override UserControl GetConfigControl()
        {
            MainWindow.ClientConnections = "100";
            return _control;
        }


        public override void StartServer(int clientConnections)
        {

            Log("Starting Test Server");

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
                    Log("New Test Client Connected");
                };

                _testServer.Closed += (sender, args) =>
                {
                    Log($"Test Client Disconnected. Reason: {args.CloseReason}");
                };
            }

            _testServer.Start();

            base.StartServer(clientConnections);
        }


        protected override void OnServerReady(SessionEventArgs<ControllerSession, RpcConfig> args)
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                args.Session.GetProxy<IControllerService>()
                    .StartConnectionTest(ClientConnections, _control.ConfigBytesPerMessage,
                        _control.ConfigMessagePeriod);
            });
        }



        public override void StopTest()
        {
            base.StopTest();
            _testServer?.Stop();
        }

    }

}
