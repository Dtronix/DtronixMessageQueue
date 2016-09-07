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

			RpcSingleProcessTest(5, 500, config);
		}


		private void RpcSingleProcessTest(int runs, int loops, MqSocketConfig config, RpcTestType type) {
			var server = new RpcServer<SimpleRpcSession>(config);
			TestService test_service;

			server.Connected += (sender, args) => {
				
				test_service = new TestService();
				args.Session.AddService(test_service);
			};


			server.Start();

			double[] total_values = { 0 };

			var count = 0;
			var sw = new Stopwatch();
			var wait = new AutoResetEvent(false);
			var complete_test = new AutoResetEvent(false);
			var client = new RpcClient<SimpleRpcSession>(config);

			Console.WriteLine("|   Build |   Calls    | Milliseconds |    RPC/sec |");
			Console.WriteLine("|---------|------------|--------------|------------|");



			client.Connected += (sender, args) => {
				args.Session.AddProxy<ITestService>(new TestService());
				var service = client.Session.GetProxy<ITestService>();
			};



			server.IncomingMessage += (sender, args2) => {
				count += args2.Messages.Count;


				if (count == runs) {
					sw.Stop();
					var mode = "Release";
#if DEBUG
					mode = "Debug";
#endif

					var messages_per_second = (int)((double)runs / sw.ElapsedMilliseconds * 1000);
					Console.WriteLine("| {0,7} | {1,10:N0} | {2,12:N0} | {3,8:N2} |", mode, runs,
						sw.ElapsedMilliseconds, messages_per_second);
					total_values[0] += messages_per_second;


					wait.Set();
				}

			};



			var send = new Action(() => {
				count = 0;
				sw.Restart();
				for (var i = 0; i < runs; i++) {
					client.Send(message);
				}
				//MqServer sv = server;
				wait.WaitOne();
				wait.Reset();

			});

			client.Connected += (sender, args) => {
				for (var i = 0; i < loops; i++) {
					send();
				}

				Console.WriteLine("|         |            |  AVERAGES | {0,12:N0} | {1,10:N0} | {2,8:N2} |", total_values[0] / loops,
					total_values[1] / loops, total_values[2] / loops);
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
