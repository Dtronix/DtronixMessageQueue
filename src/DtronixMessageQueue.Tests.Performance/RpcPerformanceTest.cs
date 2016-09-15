using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Tests.Performance.Services.Server;

namespace DtronixMessageQueue.Tests.Performance {
	class RpcPerformanceTest {
		public RpcPerformanceTest(string[] args) {
			var config = new RpcConfig {
				Ip = "127.0.0.1",
				Port = 2828
			};

			RpcSingleProcessTest(100000, 4, config, RpcTestType.NoRetrun);

			RpcSingleProcessTest(10000, 4, config, RpcTestType.Return);

			RpcSingleProcessTest(10000, 4, config, RpcTestType.Exception);


		}


		private void RpcSingleProcessTest(int runs, int loops, RpcConfig config, RpcTestType type) {
			var server = new RpcServer<SimpleRpcSession, RpcConfig>(config);
			TestService test_service;
			double[] total_values = { 0, 0 };
			var sw = new Stopwatch();
			var wait = new AutoResetEvent(false);
			var complete_test = new AutoResetEvent(false);
			var client = new RpcClient<SimpleRpcSession, RpcConfig>(config);

			server.Connected += (sender, args) => {
				test_service = new TestService();
				args.Session.AddService(test_service);

				test_service.Completed += (o, session) => {
					sw.Stop();
					var mode = "Release";
#if DEBUG
					mode = "Debug";
#endif

					var messages_per_second = (int)((double)runs / sw.ElapsedMilliseconds * 1000);
					Console.WriteLine("| {0,7} | {1,9} | {2,10:N0} | {3,12:N0} |   {4,8:N0} |", mode, type, runs, sw.ElapsedMilliseconds, messages_per_second);
					total_values[0] += sw.ElapsedMilliseconds;
					total_values[1] += messages_per_second;


					wait.Set();
				};
				
			};


			server.Start();


			Console.WriteLine("|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |");
			Console.WriteLine("|---------|-----------|------------|--------------|------------|");



			var send = new Action(() => {
				
				var service = client.Session.GetProxy<ITestService>();
				service.ResetTest();

				sw.Restart();
				for (var i = 0; i < runs; i++) {
					switch (type) {
						case RpcTestType.NoRetrun:
							service.TestNoReturn();
							break;

						case RpcTestType.Return:
							service.TestIncrement();
							break;

						case RpcTestType.Exception:
							try {
								service.TestException();
							} catch {
								//ignored
							}
							
							break;
					}
					
				}

				wait.WaitOne();
				wait.Reset();
			});


			client.Connected += (sender, args) => {
				args.Session.AddProxy<ITestService>(new TestService());
				var service = client.Session.GetProxy<ITestService>();
				service.TestSetup(runs);

				for (var i = 0; i < loops; i++) {
					send();
				}

				Console.WriteLine("|         |           |   AVERAGES | {0,12:N0} | {1,10:N0} |", total_values[0]/loops, total_values[1]/loops);
				Console.WriteLine();

				server.Stop();
				client.Close();
				complete_test.Set();
			};

			client.Connect();

			complete_test.WaitOne();
		}
	}

	enum RpcTestType {
		NoRetrun,
		Return,
		Exception,
	}
}
