using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Tests.Rpc.Services.Server;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Rpc {
	public class RpcClientTests : RpcTestsBase {


		public RpcClientTests(ITestOutputHelper output) : base(output) {

		}

		public class Test {
			public string TestStr { get; set; }
			public int Length { get; set; }

		}

		[Fact]
		public void Client_calls_proxy_method() {

			Server.Connected += (sender, args) => {
				args.Session.AddService(new CalculatorService());
			};


			Client.Connected += (sender, args) => {
				args.Session.AddProxy<ICalculatorService>(new CalculatorService());
				var service = Client.Session.GetProxy<ICalculatorService>();
				var result = service.Add(100, 200);

				if (result != 300) {
					LastException = new Exception("Service returned wrong result.");
				}

				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Client_calls_proxy_method_sequential() {

			Server.Connected += (sender, args) => {
				args.Session.AddService<ICalculatorService>(new CalculatorService());
			};


			Client.Connected += (sender, args) => {
				args.Session.AddProxy<ICalculatorService>(new CalculatorService());
				var service = Client.Session.GetProxy<ICalculatorService>();
				Stopwatch stopwatch = Stopwatch.StartNew();

				int added_int = 0;
				for (int i = 0; i < 10; i++) {
					added_int = service.Add(added_int, 1);
				}

				Output.WriteLine($"{stopwatch.ElapsedMilliseconds}");
				TestStatus.Set();
			};

			StartAndWait();
		}

		[Fact]
		public void Client_calls_proxy_method_and_canceles() {

			Server.Connected += (sender, args) => {
				var service = new CalculatorService();
				args.Session.AddService<ICalculatorService>(service);

				service.LongRunningTaskCanceled += (o, event_args) => {
					TestStatus.Set();
				};
			};


			Client.Connected += (sender, args) => {
				args.Session.AddProxy<ICalculatorService>(new CalculatorService());
				var service = Client.Session.GetProxy<ICalculatorService>();
				var token_source = new CancellationTokenSource();

				token_source.CancelAfter(200);
				bool threw = false;
				try {
					service.LongRunningTask(1, 2, token_source.Token);
				} catch (OperationCanceledException) {
					threw = true;
				}

				if (threw != true) {
					LastException = new Exception("Operation did not cancel.");
				}
				
				
			};

			StartAndWait();
		}


	}
}
