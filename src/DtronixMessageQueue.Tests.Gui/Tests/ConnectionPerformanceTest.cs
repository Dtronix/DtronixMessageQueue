using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class ConnectionPerformanceTest : PerformanceTest
    {

        private RpcServer<ControllerSession, RpcConfig> _server;
        public ConnectionPerformanceTest() : base("Connection Test")
        {
            
        }

        public override UserControl GetConfigControl()
        {
            return null;
        }




        public override void StartClient(int clientProcesses)
        {
            Process.Start("DtronixMessageQueue.Tests.Gui.exe", "client");
        }

        public override void StartServer(int clientProcesses, int clientConnections)
        {
            throw new NotImplementedException();
        }
    }
}
