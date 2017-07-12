using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class MaxThroughputPerformanceTest : PerformanceTest
    {


        private MqServer<MaxThroughputPerformanceTestSession, MqConfig> _testServer;
        
        private MaxThroughputPerformanceTestControl _control;



        public MaxThroughputPerformanceTest(MainWindow mainWindow) : base("Max Throughput Test", mainWindow)
        {
            _control = new MaxThroughputPerformanceTestControl();
        }

        public override UserControl GetConfigControl()
        {
            MainWindow.ClientConnections = "1";
            return _control;
        }

        public override void StartServer(int clientConnections)
        {

            Log("Starting Test Server");

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
                    .StartMaxThroughputTest(ClientConnections, _control.ConfigFrames,
                        _control.ConfigFrameSize);
            });
        }

        public override void StopTest()
        {
            base.StopTest();
            _testServer?.Stop();
        }
    }
    
}
