﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Tests.Performance.Services.Server;

namespace DtronixMessageQueue.Tests.Performance
{
    class RpcPerformanceTest
    {
        public RpcPerformanceTest(string[] args)
        {
            var config = new RpcConfig
            {
                Address = "127.0.0.1:2828",
            };

            //RpcSingleProcessTest(20, 4, config, RpcTestType.LngBlock);

            RpcSingleProcessTest(200000, 4, config, RpcTestType.NoReturn);

            RpcSingleProcessTest(200000, 4, config, RpcTestType.Await);

            //RpcSingleProcessTest(100, 4, config, RpcTestType.Block);

            RpcSingleProcessTest(10000, 4, config, RpcTestType.Return);

            RpcSingleProcessTest(2000, 4, config, RpcTestType.Exception);


        }


        private void RpcSingleProcessTest(int runs, int loops, RpcConfig config, RpcTestType type)
        {
            var server = new RpcServer<SimpleRpcSession, RpcConfig>(config, null);
            TestService test_service;
            double[] total_values = {0, 0};
            var sw = new Stopwatch();
            var wait = new AutoResetEvent(false);
            var complete_test = new AutoResetEvent(false);
            var client = new RpcClient<SimpleRpcSession, RpcConfig>(config);

            server.SessionSetup += (sender, args) =>
            {
                test_service = new TestService();
                args.Session.AddService(test_service);

                test_service.Completed += (o, session) =>
                {
                    sw.Stop();
                    var mode = "Release";
#if DEBUG
                    mode = "Debug";
#endif

                    var messages_per_second = (int) ((double) runs / sw.ElapsedMilliseconds * 1000);
                    Console.WriteLine("| {0,7} | {1,9} | {2,10:N0} | {3,12:N0} |   {4,8:N0} |", mode, type, runs,
                        sw.ElapsedMilliseconds, messages_per_second);
                    total_values[0] += sw.ElapsedMilliseconds;
                    total_values[1] += messages_per_second;


                    wait.Set();
                };

            };


            server.Start();


            Console.WriteLine("|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |");
            Console.WriteLine("|---------|-----------|------------|--------------|------------|");



            var send = new Action(() =>
            {
                
                var service = client.Session.GetProxy<ITestService>();
                service.ResetTest();

                sw.Restart();
                for (var i = 0; i < runs; i++)
                {
                    switch (type)
                    {

                        case RpcTestType.LngBlock:
                            service.TestNoReturnLongBlocking();
                            break;

                        case RpcTestType.Block:
                            service.TestNoReturnBlock();
                            break;

                        case RpcTestType.NoReturn:
                            service.TestNoReturn();
                            break;

                        case RpcTestType.Await:
                            service.TestNoReturnAwait();
                            break;

                        case RpcTestType.Return:
                            service.TestIncrement();
                            break;

                        case RpcTestType.Exception:
                            try
                            {
                                service.TestException();
                            }
                            catch
                            {
                                //ignored
                            }

                            break;
                    }

                }

                wait.WaitOne();
                wait.Reset();
            });


            client.Ready += (sender, args) =>
            {
                Thread.Sleep(300);
                args.Session.AddProxy<ITestService>("TestService");
                var service = client.Session.GetProxy<ITestService>();
                service.TestSetup(runs);

                for (var i = 0; i < loops; i++)
                    send();

                // Added to ensure that the averages are outputted after the final result.
                Thread.Sleep(100);

                Console.WriteLine("|         |           |   AVERAGES | {0,12:N0} | {1,10:N0} |",
                    total_values[0] / loops, total_values[1] / loops);
                Console.WriteLine();

                server.Stop();
                client.Close();
                complete_test.Set();
            };

            client.Connect();

            complete_test.WaitOne();
        }
    }

    enum RpcTestType
    {
        NoReturn,
        Return,
        Exception,
        Await,
        Block,
        LngBlock
    }
}
