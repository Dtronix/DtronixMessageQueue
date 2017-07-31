﻿using System;
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
        private readonly TestController _testController;

        public string Name { get; } = "ControllerService";
        public ControllerSession Session { get; set; }
        private RpcServer<ControllerSession, RpcConfig> _server;

        private List<MqClient<ConnectionPerformanceTestSession, MqConfig>> _connectionTestClientList;
        
        private PerformanceTest TestBase;

        public ControllerService(TestController testController)
        {
            _testController = testController;
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
            var test = (T)_testController.MainWindow.PerformanceTests.FirstOrDefault(pt => pt is T);

            return test;
        }

        private T SetClientTest<T>() where T : PerformanceTest
        {
            var test = GetClientTest<T>();

            _testController.MainWindow.SelectedPerformanceTest = test;

            if (test != null)
                _testController.Log("Activating " + test.Name);


            return test;
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
            _testController.MainWindow.Dispatcher.Invoke(() =>
            {
                _testController.MainWindow.Close();
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

        public void StartConnectionTest(int clients, int bytesPerMessage, int messagePeriod)
        {

            _testController.MainWindow.Dispatcher.Invoke(() =>
            {
                var test = SetClientTest<ConnectionPerformanceTest>();

                test.ActualControl.ConfigClients = clients;
                test.ActualControl.ConfigMessagePeriod = messagePeriod;
                test.ActualControl.ConfigBytesPerMessage = bytesPerMessage;

                test.StartTestClient();
            });
        }

        public void StartMaxThroughputTest(int clients, int frames, int frameSize)
        {

            _testController.MainWindow.Dispatcher.Invoke(() =>
            {
                var test = SetClientTest<MaxThroughputPerformanceTest>();

                test.ActualControl.ConfigClients = clients;
                test.ActualControl.ConfigFrames = frames;
                test.ActualControl.ConfigFrameSize = frameSize;

                test.StartTestClient();
            });
        }

        public void PauseTest()
        {

            _testController.MainWindow.Dispatcher.Invoke(() =>
            {
                _testController.MainWindow.SelectedPerformanceTest.PauseTest();
            });
        }
    }
}
