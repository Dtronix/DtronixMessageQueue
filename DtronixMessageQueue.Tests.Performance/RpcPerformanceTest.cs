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
			var config = new MqSocketConfig() {
				Ip = "127.0.0.1",
				Port = 2828
			};

			RpcSingleProcessTest(1000, 10, config, RpcTestType.NoRetrun);
		}


		private void RpcSingleProcessTest(int runs, int loops, MqSocketConfig config, RpcTestType type) {
			var server = new RpcServer<SimpleRpcSession>(config);
			TestService test_service;
			double[] total_values = { 0, 0 };
			var sw = new Stopwatch();
			var wait = new AutoResetEvent(false);
			var complete_test = new AutoResetEvent(false);
			var client = new RpcClient<SimpleRpcSession>(config);

			server.Connected += (sender, args) => {
				
				test_service = new TestService();
				test_service.Completed += (o, session) => {
					sw.Stop();
					var mode = "Release";
#if DEBUG
					mode = "Debug";
#endif

					var messages_per_second = (int)((double)runs / sw.ElapsedMilliseconds * 1000);
					Console.WriteLine("| {0,7} | {1,10:N0} | {2,12:N0} |   {3,8:N0} |", mode, runs, sw.ElapsedMilliseconds, messages_per_second);
					total_values[0] += sw.ElapsedMilliseconds;
					total_values[1] += messages_per_second;


					wait.Set();
				};
				args.Session.AddService(test_service);
			};


			server.Start();


			Console.WriteLine("|   Build |   Calls    | Milliseconds |    RPC/sec |");
			Console.WriteLine("|---------|------------|--------------|------------|");



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

				Console.WriteLine("|         |   AVERAGES | {0,12:N0} | {1,10:N0} |", total_values[0]/loops, total_values[1]/loops);
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
