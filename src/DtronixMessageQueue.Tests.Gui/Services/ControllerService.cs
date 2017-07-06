using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Tests;

namespace DtronixMessageQueue.Tests.Gui.Services
{
    class ControllerService : IControllerService
    {
        public string Name { get; } = "ControllerService";
        public ControllerSession Session { get; set; }
        private RpcServer<ControllerSession, RpcConfig> _server;

        private List<MqClient<ConnectionPerformanceTestSession, MqConfig>> _connectionTestClientList;

        public ControllerService()
        {
            _connectionTestClientList = new List<MqClient<ConnectionPerformanceTestSession, MqConfig>>();
        }

        public void ClientReady()
        {
            if (Session.BaseSocket.Mode == SocketMode.Server && _server == null)
            {
                _server = (RpcServer<ControllerSession, RpcConfig>) this.Session.BaseSocket;
            }
        }



        public void StartConnectionTest(string ip, int port, int clients, int packageLength, int perioid)
        {

            if (_connectionTestClientList != null)
                return;

            for (int i = 0; i < clients; i++)
            {
                var client = new MqClient<ConnectionPerformanceTestSession, MqConfig>(new MqConfig()
                {
                    Ip = ip,
                    Port = port
                });

                client.Connected += (sender, args) =>
                {
                    args.Session.StartTest();
                };

                client.Connect();

                _connectionTestClientList.Add(client);
            }

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
    }
}
