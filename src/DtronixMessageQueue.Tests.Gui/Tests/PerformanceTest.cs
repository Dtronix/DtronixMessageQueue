using System;
using System.Threading;
using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public abstract class PerformanceTest
    {
        public string Name { get; }
        public TestController TestController { get; private set; }

        protected RpcServer<ControllerSession, RpcConfig> ControlServer;
        protected RpcClient<ControllerSession, RpcConfig> ControllClient;

        public long ServerThroughput { get; set; }

        protected ControllerService ControllerService;

        public UserControl Control { get; protected set; }

        public int TotalConnections;

        protected PerformanceTest(string name, TestController testController)
        {
            Name = name;
            TestController = testController;
        }

        protected void ConnectionAdded()
        {
            TestController.Log("Connection test client connected.");
            Interlocked.Increment(ref TotalConnections);
            Update();
        }

        protected void ConnectionRemoved(SocketCloseReason reason)
        {
            TestController.Log($"Test client connection closed. Reason: {reason}");
            Interlocked.Decrement(ref TotalConnections);
            Update();
        }

        protected abstract void Update();


        public abstract void StartClient();

        public abstract void StartServer();


        protected abstract void OnClientSession(SessionEventArgs<ControllerSession, RpcConfig> args);

        public virtual void StopTest()
        {
            if (ControlServer != null)
            {
                TestController.Log("Stopped Test");
                var sessions = ControlServer.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().StopTest();
                }

                ControlServer.Stop();
            }

            if (ControllClient != null)
            {
                ControllerService.StopTest();
                ControllClient.Close();
            }
        }

        public virtual void PauseTest()
        {
            if (ControlServer != null)
            {
                var sessions = ControlServer.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().PauseTest();
                }
            }
        }

        public virtual void CloseConnectedClients()
        {
            if (ControlServer != null)
            {
                var sessions = ControlServer.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().CloseClient();
                }
            }
        }
    }
}