using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public abstract class PerformanceTest
    {
        public string Name { get; }


        protected PerformanceTest(string name)
        {
            Name = name;
        }

        public abstract UserControl GetConfigControl();
        public abstract void StartServer(int clientProcesses, int clientConnections);
        public abstract void StartClient(int clientConnections);
    }
}