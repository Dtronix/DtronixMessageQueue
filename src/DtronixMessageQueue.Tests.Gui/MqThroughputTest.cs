using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using DtronixMessageQueue.Tests.Gui.TestSessions;

namespace DtronixMessageQueue.Tests.Gui
{
    class MqThroughputTest : PerformanceTestBase
    {

        List<MqThroughputTestSession> clients = new List<MqThroughputTestSession>();

        private Timer _reportTimer;
        private MqServer<MqThroughputTestSession, MqConfig> server;

        public MqThroughputTest(string[] args)
        {

            string mode = args.Length < 2 ? "in-process" : args[1];
            string ip = "127.0.0.1";
            int totalFrames = 10;
            int frameSize = 1024;
            int totalClients = 3;

            if (args.Length == 6)
            {
                ip = args[2];
                totalFrames = int.Parse(args[3]);
                frameSize = int.Parse(args[4]);
                totalClients = int.Parse(args[5]);
            }


            if (mode == "setup")
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                Process.Start(exePath,
                    $"mq-throughput server {ip} {totalFrames} {frameSize} {totalClients}");

                for (int i = 0; i < 1; i++)
                {
                    Process.Start(exePath,
                        $"mq-throughput client {ip} {totalFrames} {frameSize} {totalClients}");
                }
            }
            else if (mode == "server")
            {
                StartServer();

                _reportTimer = new Timer(DisplayServerStatus);
            }
            else if (mode == "client")
            {

                for (int i = 0; i < totalClients; i++)
                {
                    StartClient(ip, totalFrames, frameSize);
                }

                _reportTimer = new Timer(DisplayClientStatus);

            }
            else if (mode == "in-process")
            {

                StartServer();

                for (int i = 0; i < totalClients; i++)
                {
                    StartClient(ip, totalFrames, frameSize);
                }

                _reportTimer = new Timer(DisplayClientStatus);

            }
            else
            {
                Console.WriteLine("Invalid parameters passed to performance tester");
            }

            _reportTimer.Change(1000, 1000);
        }


        private void StartClient(string ip, int totalFrames, int frameSize)
        {
            var cl = new MqClient<MqThroughputTestSession, MqConfig>(new MqConfig{
                Ip = ip,
                Port = 2828
            });

            cl.SessionSetup += (sender, args) =>
            {
                args.Session.IsServer = false;
                args.Session.ConfigTest(frameSize, totalFrames);
                
            };

            cl.Connected += (sender, args) =>
            {
                clients.Add(args.Session);
                args.Session.StartTest();
            };

            cl.Connect();

           
        }


        private void StartServer()
        {
            server = new MqServer<MqThroughputTestSession, MqConfig>(new MqConfig
            {
                Ip = "127.0.0.1",
                Port = 2828
            });


            server.SessionSetup += (sender, args) =>
            {
                args.Session.IsServer = true;
            };

            server.Connected += (sender, args) =>
            {
                args.Session.StartTest();
            };


            server.Start();
        }


        private void DisplayClientStatus(object state)
        {
            Console.Clear();

            Console.WriteLine($"Clients Test Running.\r\n");
            Console.WriteLine("| Messages | Frames   | Time     | Msg/sec  | Mbps     | Runtime  |");
            Console.WriteLine("|----------|----------|----------|----------|----------|----------|");

            foreach (var client in clients)
            {
                if (client.TotalThroughTime == 0)
                    continue;
                    
                var messagesPerSecond = (int)((double)client.TotalThroughMessages / client.TotalThroughTime * 1000);
                var mbps = (double)client.TotalThroughBytes / client.TotalThroughTime / 1000;
                //var mbps = runs * (double)(msgSizeNoHeader) / sw.ElapsedMilliseconds / 1000;
                Console.WriteLine("| {0,8} | {1,8} | {2,8:N0} | {3,8:N0} | {4,8:N} | {5,8:N} |",
                    client.TotalThroughMessages,
                    client.TotalThroughFrames,
                    client.TotalThroughTime,
                    messagesPerSecond,
                    mbps,
                    (DateTime.Now - client.StartedTime).TotalMilliseconds/1000);

                /*if ((DateTime.Now - client.StartedTime).TotalSeconds > 10)
                {
                    client.CancelTest = true;
                }*/
            }
        }

        private void DisplayServerStatus(object state)
        {
            Console.Clear();

            Console.WriteLine($"Server Running.");

            int c = 0;
            using (var e = server.GetSessionsEnumerator())
            {
                while (e.MoveNext())
                    c++;
            }

            Console.WriteLine($"Total Clients: {c}");

        }
    }
}