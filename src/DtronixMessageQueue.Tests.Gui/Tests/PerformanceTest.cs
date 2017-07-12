using System.Windows.Controls;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public abstract class PerformanceTest
    {
        public string Name { get; }
        public MainWindow MainWindow { get; }

        protected RpcServer<ControllerSession, RpcConfig> Server;
        protected RpcClient<ControllerSession, RpcConfig> Client;


        protected int ClientConnections;
        public long ServerThroughput { get; set; }

        protected ControllerService _controllerService;

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


        public virtual void StartClient(string ip)
        {

            Log("Starting Test Client");
            Client = new RpcClient<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = ip,
                Port = 2120,
                RequireAuthentication = false,
                PingFrequency = 800
            });

            Client.SessionSetup += OnClientSessionSetup;

            Client.Connect();
        }

        public virtual void StartServer(int clientConnections)
        {
            Log("Starting Controlling Server");
            ClientConnections = clientConnections;
            Server = new RpcServer<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = "0.0.0.0",
                Port = 2120,
                RequireAuthentication = false,
                PingTimeout = 1000
            });

            Server.Ready += (sender, args) =>
            {
                Log("Client Connected");
                OnServerReady(args);

            };

            Server.SessionSetup += OnServerSessionSetup;
            Server.Closed += ServerOnClosed;

            Server.Start();
        }

        protected abstract void OnServerReady(SessionEventArgs<ControllerSession, RpcConfig> args);

        public virtual void StopTest()
        {
            if (Server != null)
            {
                Log("Stopped Test");
                var sessions = Server.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().StopTest();
                }
            }

            if (Client != null)
            {
                _controllerService.StopTest();
                Client.Close();
            }
        }

        public virtual void CloseConnectedClients()
        {
            if (Server != null)
            {
                var sessions = Server.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().CloseClient();
                }
            }
        }

        protected void ServerOnClosed(object sender, SessionClosedEventArgs<ControllerSession, RpcConfig> sessionClosedEventArgs)
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
    }
}