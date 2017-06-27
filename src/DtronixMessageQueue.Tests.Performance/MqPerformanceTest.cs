using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace DtronixMessageQueue.Tests.Performance
{
    class MqPerformanceTest : PerformanceTestBase
    {
        public MqPerformanceTest(string[] args)
        {
            string mode = null;
            int totalLoops, totalMessages, totalFrames, frameSize, totalClients;

            if (args == null || args.Length == 0)
            {
                mode = "single-process";
                totalLoops = 1;
                totalMessages = 1000000;
                totalFrames = 4;
                frameSize = 50;
                totalClients = 1;
            }
            else if (args.Length == 7)
            {
                mode = args[1];
                totalLoops = int.Parse(args[2]);
                totalMessages = int.Parse(args[3]);
                totalFrames = int.Parse(args[4]);
                frameSize = int.Parse(args[5]);
                totalClients = int.Parse(args[6]);
            }
            else
            {
                Console.WriteLine("Invalid parameters passed to performance tester");
                return;
            }

            var exePath = Assembly.GetExecutingAssembly().Location;

            if (mode == "setup")
            {
                Process.Start(exePath,
                    $"mq server {totalLoops} {totalMessages} {totalFrames} {frameSize} {totalClients}");

                for (int i = 0; i < totalClients; i++)
                {
                    Process.Start(exePath,
                        $"mq client {totalLoops} {totalMessages} {totalFrames} {frameSize} {totalClients}");
                }
            }
            else if (mode == "client")
            {
                Console.WriteLine("|   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |");
                Console.WriteLine("|------------|-----------|--------------|------------|----------|");

                StartClient(totalLoops, totalMessages, totalFrames, frameSize);
            }
            else if (mode == "server")
            {
                StartServer(totalMessages, totalClients);
            }
            else if (mode == "single-process")
            {
                MqInProcessTest();
            }
        }


        private static void StartClient(int totalLoops, int totalMessages, int totalFrames, int frameSize)
        {
            var cl = new MqClient<SimpleMqSession, MqConfig>(new MqConfig()
            {
                Ip = "127.0.0.1",
                Port = 2828
            });

            var stopwatch = new Stopwatch();
            var messageReader = new MqMessageReader();
            var messageSize = totalFrames * frameSize;
            var message = new MqMessage();
            double[] totalValues = {0, 0, 0};

            for (int i = 0; i < totalFrames; i++)
            {
                message.Add(new MqFrame(SequentialBytes(frameSize), MqFrameType.More, (MqConfig) cl.Config));
            }

            cl.IncomingMessage += (sender, args) =>
            {
                MqMessage msg;
                while (args.Messages.Count > 0)
                {
                    msg = args.Messages.Dequeue();

                    messageReader.Message = msg;
                    var result = messageReader.ReadString();

                    if (result == "COMPLETE")
                    {
                        if (totalLoops-- > 0)
                        {
                            stopwatch.Stop();

                            var messagesPerSecond =
                                (int) ((double) totalMessages / stopwatch.ElapsedMilliseconds * 1000);
                            var msgSizeNoHeader = messageSize;
                            var mbps = totalMessages * (double) (msgSizeNoHeader) / stopwatch.ElapsedMilliseconds / 1000;
                            Console.WriteLine("| {0,10:N0} | {1,9:N0} | {2,12:N0} | {3,10:N0} | {4,8:N2} |",
                                totalMessages,
                                msgSizeNoHeader, stopwatch.ElapsedMilliseconds, messagesPerSecond, mbps);

                            totalValues[0] += stopwatch.ElapsedMilliseconds;
                            totalValues[1] += messagesPerSecond;
                            totalValues[2] += mbps;
                        }

                        if (totalLoops == 0)
                        {
                            Console.WriteLine("|            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |",
                                totalValues[0] / totalLoops,
                                totalValues[1] / totalLoops, totalValues[2] / totalLoops);
                            Console.WriteLine();
                            Console.WriteLine("Test complete");
                        }


                        cl.Close();
                    }
                    else if (result == "START")
                    {
                        if (totalLoops > 0)
                        {
                            stopwatch.Restart();
                            for (var i = 0; i < totalMessages; i++)
                            {
                                cl.Send(message);
                            }
                        }
                    }
                }
            };

            cl.Connect();
        }

        private static void StartServer(int totalMessages, int totalClients)
        {
            var server = new MqServer<SimpleMqSession, MqConfig>(new MqConfig()
            {
                Ip = "127.0.0.1",
                Port = 2828
            });

            var builder = new MqMessageWriter((MqConfig) server.Config);
            builder.Write("COMPLETE");

            var completeMessage = builder.ToMessage(true);

            builder.Write("START");
            var startMessage = builder.ToMessage(true);

            ConcurrentDictionary<SimpleMqSession, ClientRunInfo> clientsInfo =
                new ConcurrentDictionary<SimpleMqSession, ClientRunInfo>();


            server.Connected += (sender, session) =>
            {
                var currentInfo = new ClientRunInfo()
                {
                    Session = session.Session,
                    Runs = 0
                };
                clientsInfo.TryAdd(session.Session, currentInfo);

                if (clientsInfo.Count == totalClients)
                {
                    foreach (var mqSession in clientsInfo.Keys)
                    {
                        mqSession.Send(startMessage);
                    }
                }
            };

            server.Closed += (session, value) =>
            {
                ClientRunInfo info;
                clientsInfo.TryRemove(value.Session, out info);
            };

            server.IncomingMessage += (sender, args) =>
            {
                var clientInfo = clientsInfo[args.Session];

                // Count the total messages.
                clientInfo.Runs += args.Messages.Count;

                if (clientInfo.Runs == totalMessages)
                {
                    args.Session.Send(completeMessage);
                    args.Session.Send(startMessage);
                    clientInfo.Runs = 0;
                }
            };


            server.Start();
        }


        static void MqInProcessTest()
        {
            var config = new MqConfig
            {
                Ip = "127.0.0.1",
                Port = 2828
            };


            Console.WriteLine("FrameBufferSize: {0}; SendAndReceiveBufferSize: {1}\r\n", config.FrameBufferSize,
                config.SendAndReceiveBufferSize);

            var smallMessage = new MqMessage
            {
                new MqFrame(SequentialBytes(50), MqFrameType.More, config),
                new MqFrame(SequentialBytes(50), MqFrameType.More, config),
                new MqFrame(SequentialBytes(50), MqFrameType.More, config),
                new MqFrame(SequentialBytes(50), MqFrameType.Last, config)
            };

            MqInProcessPerformanceTests(1000000, 5, smallMessage, config);

            var medimumMessage = new MqMessage
            {
                new MqFrame(SequentialBytes(500), MqFrameType.More, config),
                new MqFrame(SequentialBytes(500), MqFrameType.More, config),
                new MqFrame(SequentialBytes(500), MqFrameType.More, config),
                new MqFrame(SequentialBytes(500), MqFrameType.Last, config)
            };

            MqInProcessPerformanceTests(100000, 5, medimumMessage, config);

            var largeMessage = new MqMessage();

            for (int i = 0; i < 20; i++)
            {
                largeMessage.Add(new MqFrame(SequentialBytes(3000), MqFrameType.More, config));
            }

            MqInProcessPerformanceTests(10000, 5, largeMessage, config);

            Console.WriteLine("Performance complete");

            Console.ReadLine();
        }

        private static void MqInProcessPerformanceTests(int runs, int loops, MqMessage message, MqConfig config)
        {
            var server = new MqServer<SimpleMqSession, MqConfig>(config);
            server.Start();

            double[] totalValues = {0, 0, 0};

            var count = 0;
            var sw = new Stopwatch();
            var wait = new AutoResetEvent(false);
            var completeTest = new AutoResetEvent(false);

            var client = new MqClient<SimpleMqSession, MqConfig>(config);

            Console.WriteLine("|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |");
            Console.WriteLine("|---------|------------|-----------|--------------|------------|----------|");


            var messageSize = message.Size;

            server.IncomingMessage += (sender, args2) =>
            {
                count += args2.Messages.Count;


                if (count == runs)
                {
                    sw.Stop();
                    var mode = "Release";

#if DEBUG
					mode = "Debug";
#endif

                    var messagesPerSecond = (int) ((double) runs / sw.ElapsedMilliseconds * 1000);
                    var msgSizeNoHeader = messageSize - 12;
                    var mbps = runs * (double) (msgSizeNoHeader) / sw.ElapsedMilliseconds / 1000;
                    Console.WriteLine("| {0,7} | {1,10:N0} | {2,9:N0} | {3,12:N0} | {4,10:N0} | {5,8:N2} |", mode, runs,
                        msgSizeNoHeader, sw.ElapsedMilliseconds, messagesPerSecond, mbps);
                    totalValues[0] += sw.ElapsedMilliseconds;
                    totalValues[1] += messagesPerSecond;
                    totalValues[2] += mbps;


                    wait.Set();
                }
            };


            var send = new Action(() =>
            {
                count = 0;
                sw.Restart();
                for (var i = 0; i < runs; i++)
                {
                    client.Send(message);
                }
                //MqServer sv = server;
                wait.WaitOne();
                wait.Reset();
            });

            client.Connected += (sender, args) =>
            {
                for (var i = 0; i < loops; i++)
                {
                    send();
                }

                Console.WriteLine("|         |            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |",
                    totalValues[0] / loops,
                    totalValues[1] / loops, totalValues[2] / loops);
                Console.WriteLine();

                server.Stop();
                client.Close();
                completeTest.Set();
            };

            client.Connect();

            completeTest.WaitOne();
        }
    }
}