﻿using System;
using System.Diagnostics;
using System.Threading;
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
				for (int i = 0; i < 2; i++) {
					added_int = service.Add(added_int, 1);
				}
				
				Output.WriteLine($"{stopwatch.ElapsedMilliseconds}");
				TestStatus.Set();
			};

			StartAndWait();
		}



		[Fact]
		public void serializes_data() {
			var writer = new MqMessageWriter(Config);
			var bwriter = new BsonWriter(writer) {CloseOutput = false};

			var s = new Test {
				Length = 51235115,
				TestStr = "ASFSFASFsfaasf aslgheqw8tyh 23  hy wasgh asdgio a"
			};

			JsonSerializer serializer = new JsonSerializer();
			serializer.Serialize(bwriter, s);


			var message2 = writer.ToMessage();
		}
	}
}
