using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class ConnectionPerformanceTest : PerformanceTest
    {

        private RpcServer<ControllerSession, RpcConfig> _server;
        private RpcClient<ControllerSession, RpcConfig> _client;
        private ControllerService _controllerService;

        public ConnectionPerformanceTest(MainWindow mainWindow) : base("Connection Test", mainWindow)
        {
            
        }

        public override UserControl GetConfigControl()
        {
            return null;
        }




        public override void StartClient(string ip)
        {

            Log("Starting Test Client");
            _client = new RpcClient<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = ip,
                Port = 2120,
                RequireAuthentication = false,
                PingFrequency = 1000
            });

            _client.SessionSetup += OnClientSessionSetup;

            _client.Connect();
            //
        }

        public override void StartServer(int clientConnections)
        {
            Log("Starting Server");
            _server = new RpcServer<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = "0.0.0.0",
                Port = 2120,
                RequireAuthentication = false,
                PingTimeout = 5000
            });

            _server.Ready += (sender, args) =>
            {
                Log("Client Connected");
                args.Session.GetProxy<IControllerService>().StartConnectionTest(clientConnections, 128, 3000);
            };

            _server.SessionSetup += OnServerSessionSetup;
            _server.Closed += ServerOnClosed;

            _server.Start();
        }

        private void ServerOnClosed(object sender, SessionClosedEventArgs<ControllerSession, RpcConfig> sessionClosedEventArgs)
        {
            Log($"Client Closed {sessionClosedEventArgs.CloseReason}");
        }

        protected void OnServerSessionSetup(object sender, SessionEventArgs<ControllerSession, RpcConfig> sessionEventArgs)
        {
            sessionEventArgs.Session.AddProxy<IControllerService>("ControllerService");
        }

        protected void OnClientSessionSetup(object sender, SessionEventArgs<ControllerSession, RpcConfig> sessionEventArgs)
        {
            _controllerService = new ControllerService(this);
            sessionEventArgs.Session.AddService(_controllerService);
        }



        public override void StopTest()
        {
            if (_server != null)
            {
                Log("Stopped Test");
                var sessions = _server.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().StopTest();
                }
            }else if (_client != null)
            {
                _controllerService.StopTest();
            }
        }

    }
    
}
