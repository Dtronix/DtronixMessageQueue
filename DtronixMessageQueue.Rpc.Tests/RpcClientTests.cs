﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.Tests.Services.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Rpc.Tests {
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
	}
}
