using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Tests.Performance
{
    public class ServerMqPerformanceTests
    {
        private MqServer<SimpleMqSession, MqConfig> _server;

        public ServerMqPerformanceTests(string[] args)
        {
            _server = new MqServer<SimpleMqSession, MqConfig>(new MqConfig
            {
                Ip = "127.0.0.1",
                Port = 2828
            });

            _server.SessionSetup += (sender, eventArgs) =>
            {
                eventArgs.Session.IsServer = true;
            };


        }

        public void Start()
        {
            _server.Start();
        }
    }
}
