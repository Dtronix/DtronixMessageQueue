using System;
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

        protected RpcServer<ControllerSession, RpcConfig> ControlServer;
        protected RpcClient<ControllerSession, RpcConfig> ControllClient;

        public long ServerThroughput { get; set; }

        protected ControllerService ControllerService;

        public UserControl Control { get; protected set; }

        public int TotalConnections;

        protected PerformanceTest(string name, MainWindow mainWindow)
        {
            Name = name;
            MainWindow = mainWindow;
        }

        public void Log(string message)
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                MainWindow.ConsoleOutput.AppendText($"[{DateTime.Now.ToShortTimeString()}] {message}\r");
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

        public abstract void StartClient();
        public abstract void StartServer();

        protected void StartControlClient(string ip)
        {

            Log("Starting Test ControllClient");
            ControllClient = new RpcClient<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = ip,
                Port = 2120,
                RequireAuthentication = false,
                PingFrequency = 800
            });

            ControllClient.SessionSetup += OnControllClientSessionSetup;

            ControllClient.Connect();
        }

        protected void StartControlServer()
        {
            Log("Starting Controlling ControlServer");
            ControlServer = new RpcServer<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = "0.0.0.0",
                Port = 2120,
                RequireAuthentication = false,
                PingTimeout = 1000
            });

            ControlServer.Ready += (sender, args) =>
            {
                Log("ControllClient Connected");
                OnServerReady(args);

            };

            ControlServer.SessionSetup += OnControlServerSessionSetup;
            ControlServer.Closed += ControlServerOnClosed;

            ControlServer.Start();
        }

        protected abstract void OnServerReady(SessionEventArgs<ControllerSession, RpcConfig> args);

        public virtual void StopTest()
        {
            if (ControlServer != null)
            {
                Log("Stopped Test");
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

        protected void ControlServerOnClosed(object sender, SessionClosedEventArgs<ControllerSession, RpcConfig> sessionClosedEventArgs)
        {
            Log($"ControllClient Closed {sessionClosedEventArgs.CloseReason}");
        }

        protected void OnControlServerSessionSetup(object sender, SessionEventArgs<ControllerSession, RpcConfig> sessionEventArgs)
        {
            sessionEventArgs.Session.AddProxy<IControllerService>("ControllerService");
        }

        protected void OnControllClientSessionSetup(object sender, SessionEventArgs<ControllerSession, RpcConfig> sessionEventArgs)
        {
            ControllerService = new ControllerService(this);
            sessionEventArgs.Session.AddService(ControllerService);
        }
    }
}