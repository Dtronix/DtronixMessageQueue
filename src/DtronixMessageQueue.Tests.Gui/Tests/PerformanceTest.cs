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
        public TestController TestController { get; }
        public bool Paused { get; set; }

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
        }

        protected void ConnectionRemoved(SocketCloseReason reason)
        {
            TestController.Log($"Test client connection closed. Reason: {reason}");
            Interlocked.Decrement(ref TotalConnections);
        }

        public abstract void PauseResumeTest();
        public abstract void StopTest();

        public abstract void StartClient();

        public abstract void StartServer();


        public abstract void TestControllerStartTest(ControllerSession session);

        
    }
}