using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Gui.Services;

namespace DtronixMessageQueue.Tests.Gui.Tests
{
    public class TestController
    {
        public MainWindow MainWindow { get; }
        public RpcClient<ControllerSession, RpcConfig> ControllClient { get; private set; }
        public RpcServer<ControllerSession, RpcConfig> ControlServer { get; private set; }

        public ControllerService ControllerService { get; set; }

        public event EventHandler<SessionEventArgs<ControllerSession, RpcConfig>> ClientReady;

        public event EventHandler<SessionClosedEventArgs<ControllerSession, RpcConfig>> ClientClosed;

        public PerformanceTest ActiveTest { get; private set; }



        public TestController(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
        }

        public void SetTest(PerformanceTest test)
        {
            ActiveTest = test;
        }

        public void StartClient(string ip)
        {
            if (ControllClient != null)
                return;

            Log("Starting Test Control Client");
            ControllClient = new RpcClient<ControllerSession, RpcConfig>(new RpcConfig
            {
                Ip = ip,
                Port = 2120,
                RequireAuthentication = false,
                PingFrequency = 800
            });

            ControllClient.SessionSetup += (sender, args) =>
            {
                ControllerService = new ControllerService(this);
                args.Session.AddService(ControllerService);
            };

            ControllClient.Connect();
        }

        

        public void StartServer()
        {
            if (ControlServer != null)
                return;

            Log("Starting Controlling Control Server");
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

                MainWindow.Dispatcher.Invoke(() =>
                {
                    MainWindow.SelectedPerformanceTest.TestControllerStartTest(args.Session);
                });
            };

            ControlServer.SessionSetup += (sender, args) =>
            {
                args.Session.AddProxy<IControllerService>("ControllerService");
            };

            ControlServer.Closed += (sender, args) =>
            {
                Log($"ControllClient Closed {args.CloseReason}");
                ClientClosed?.Invoke(sender, args);
            };

            ControlServer.Start();

            ActiveTest.StartServer();
        }

        public virtual void StopTest()
        {
            if (ControlServer != null)
            {
                Log("Server requesting clients stop.");
                var sessions = ControlServer.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().StopTest();
                }
            }

            if (ControllClient != null)
            {
                Log("Stopping client test.");
                ActiveTest.StopTest();
            }
        }

        public virtual void PauseTest()
        {
            if (ControlServer != null)
            {
                Log("Server requesting clients " + (ActiveTest.Paused ? "resume." : "pause."));
                ActiveTest.Paused = !ActiveTest.Paused;
                var sessions = ControlServer.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().PauseTest();
                }
            }
            if (ControllClient != null)
            {
                Log((ActiveTest.Paused ? "Resuming" : "Pausing") + " client test.");
                ActiveTest.PauseResumeTest();
                ActiveTest.Paused = !ActiveTest.Paused;
            }

            
        }

        public virtual void CloseConnectedClients()
        {
            if (ControlServer != null)
            {
                Log("Server requesting clients close.");
                var sessions = ControlServer.GetSessionsEnumerator();

                while (sessions.MoveNext())
                {
                    sessions.Current.Value.GetProxy<IControllerService>().CloseClient();
                }
            }
        }


        public void Log(string message)
        {
            MainWindow.Dispatcher.Invoke(() =>
            {
                MainWindow.ConsoleOutput.AppendText($"[{DateTime.Now.ToLongTimeString()}] {message}\r");
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
    }
}
