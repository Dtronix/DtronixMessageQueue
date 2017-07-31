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



        public TestController(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
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
                ClientReady?.Invoke(sender, args);
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

        public void Pause()
        {
            ControllerService.PauseTest();
        }
    }
}
