using System.Windows.Controls;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public abstract class PerformanceTest
    {
        public string Name { get; }
        public MainWindow MainWindow { get; }


        protected PerformanceTest(string name, MainWindow mainWindow)
        {
            Name = name;
            MainWindow = mainWindow;
        }

        public void Log(string message)
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                MainWindow.ConsoleOutput.AppendText(message + "\r");
                MainWindow.ConsoleOutput.ScrollToEnd();
            });
        }

        public void ClearLog()
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                MainWindow.ConsoleOutput.Document.Blocks.Clear();
            });
        }

        public abstract UserControl GetConfigControl();
        public abstract void StartServer(int clientConnections);
        public abstract void StartClient(string ip);
        public abstract void StopTest();
    }
}