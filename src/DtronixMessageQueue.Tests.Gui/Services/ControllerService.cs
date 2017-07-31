using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Tests;
using DtronixMessageQueue.Tests.Gui.Tests.Connection;
using DtronixMessageQueue.Tests.Gui.Tests.MaxThroughput;

namespace DtronixMessageQueue.Tests.Gui.Services
{
    public class ControllerService : IControllerService
    {
      
        public string Name { get; } = "ControllerService";
        public ControllerSession Session { get; set; }
        private RpcServer<ControllerSession, RpcConfig> _server;

        private List<MqClient<ConnectionPerformanceTestSession, MqConfig>> _connectionTestClientList;
        
        private PerformanceTest TestBase;

        public ControllerService(PerformanceTest testBase)
        {
            TestBase = testBase;
            _connectionTestClientList = new List<MqClient<ConnectionPerformanceTestSession, MqConfig>>();
        }

        public void ClientReady()
        {
            if (Session.BaseSocket.Mode == SocketMode.Server && _server == null)
            {
                _server = (RpcServer<ControllerSession, RpcConfig>) this.Session.BaseSocket;
            }
        }

        private T GetClientTest<T>() where T : PerformanceTest
        {
            var test = (T)TestBase.MainWindow.PerformanceTests.FirstOrDefault(pt => pt is T);

            return test;
        }

        private T SetClientTest<T>() where T : PerformanceTest
        {
            var test = GetClientTest<T>();

            TestBase.MainWindow.SelectedPerformanceTest = test;

            if (test != null)
                TestBase.Log("Activating " + test.Name);


            return test;
        }



        public void StartConnectionTest(int clients, int packageLength, int period)
        {
            SetClientTest<ConnectionPerformanceTest>();

            for (int i = 0; i < clients; i++)
            {
                var client = new MqClient<ConnectionPerformanceTestSession, MqConfig>(new MqConfig
                {
                    Ip = Session.Config.Ip,
                    Port = 2121,
                    PingFrequency = 500
                });

                client.Connected += (sender, args) =>
                {
                    args.Session.ConfigTest(packageLength, period);
                    TestBase.Log("Connection test client connected.");

                    ConnectionTestLog();
                    args.Session.StartTest();
                };

                client.Closed += (sender, args) =>
                {
                    ConnectionTestLog();
                };

                TestBase.Log("Connection test client connecting...");
                client.Connect();

                _connectionTestClientList.Add(client);
            }
        }

        private void ConnectionTestLog()
        {
            TestBase.ClearLog();
            TestBase.Log($"Total Connections: {_connectionTestClientList.Count}");
        }

        public void StopConnectionTest()
        {
            if (_connectionTestClientList == null)
                return;

            foreach (var mqClient in _connectionTestClientList)
            {
                mqClient.Close();
            }


        }

        public void CloseClient()
        {
            TestBase.MainWindow.Dispatcher.Invoke(() =>
            {
                TestBase.MainWindow.Close();
            });
        }

        public void StopTest()
        {
            //_connectionTestClientList.Clear();
            foreach (var mqClient in _connectionTestClientList)
            {
                mqClient.Close();
            }
        }

        public void StartMaxThroughputTest(int clients, int frames, int frameSize)
        {

            TestBase.MainWindow.Dispatcher.Invoke(() =>
            {
                var test = SetClientTest<MaxThroughputPerformanceTest>();

                test.ActualControl.ConfigClients = clients;
                test.ActualControl.ConfigFrames = frames;
                test.ActualControl.ConfigFrameSize = frameSize;

                test.StartClient();
            });
        }

        public void PauseTest()
        {

            TestBase.MainWindow.Dispatcher.Invoke(() =>
            {
                TestBase.MainWindow.SelectedPerformanceTest.PauseTest();
            });
        }
    }
}
