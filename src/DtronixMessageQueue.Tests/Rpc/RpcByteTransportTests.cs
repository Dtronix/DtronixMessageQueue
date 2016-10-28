using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Socket;
using DtronixMessageQueue.Tests.Rpc.Services.Server;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests.Rpc {
	public class RpcByteTransportTests : RpcTestsBase {


		public RpcByteTransportTests(ITestOutputHelper output) : base(output) {

		}

		[Fact]
		public void Client_creates_write_byte_transport() {

			Server.Config.RequireAuthentication = false;

			Client.Ready += (sender, e) => {
				var transport = e.Session.ByteTransport.CreateTransport();

				if (e.Session.CreatedSendByteTransport == false) {
					LastException = new Exception("Client did not create send byte transport");
				}

				TestStatus.Set();
			};


			StartAndWait();
		}

		[Fact]
		public void Server_creates_read_byte_transport() {

			Server.Config.RequireAuthentication = false;

			SimpleRpcSession server_session = null;

			Server.Ready += (sender, args) => {
				server_session = args.Session;
			};

			Client.Ready += (sender, e) => {
				var transport = e.Session.ByteTransport.CreateTransport();

				Thread.Sleep(100);

				if (server_session.CreatedReceiveByteTransport == false) {
					LastException = new Exception("Client did not create send byte transport");
				}

				TestStatus.Set();
			};


			StartAndWait();
		}

		[Fact]
		public void Client_writes_data() {

			Server.Config.RequireAuthentication = false;

			SimpleRpcSession server_session = null;
			var send_buffer = Utilities.SequentialBytes(256);

			Server.Ready += (sender, args) => {
				server_session = args.Session;
			};

			Client.Ready += async(sender, e) => {

#pragma warning disable 4014
				Task.Run(async () => {
#pragma warning restore 4014
					if (server_session.CreatedReceiveByteTransport == false) {
						LastException = new Exception("Client did not create send byte transport");
					}

					var rec_transport = server_session.ByteTransport.GetRecieveTransport(1);

					MemoryStream ms = new MemoryStream();
					byte[] buffer = new byte[2];


					int read_amount;
					while ((read_amount = await rec_transport.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)) > 0) {
						ms.Write(buffer, 0, read_amount);
					}

					try {
						Assert.Equal(send_buffer, ms.ToArray());
					} catch (Exception ex) {
						LastException = ex;
					}


					TestStatus.Set();

				});

				using (var send_transport = e.Session.ByteTransport.CreateTransport()) {
					await send_transport.WriteAsync(send_buffer, 0, send_buffer.Length, CancellationToken.None);
				}

			};


			StartAndWait();
		}

		[Fact]
		public void Client_writes_data_via_rpc() {

			Server.Config.RequireAuthentication = false;

			SimpleRpcSession server_session = null;
			var send_buffer = Utilities.SequentialBytes(1024);

			Server.SessionSetup += (sender, args) => {
				var service = new CalculatorService();
				args.Session.AddService<ICalculatorService>(service);

				service.StreamBytes = send_buffer;

				service.SuccessfulStreamTransport += (o, event_args) => {
					TestStatus.Set();
				};

				service.FailedStreamTransport += (o, event_args) => {
					LastException = new Exception("Stream did not transport all data.");
					TestStatus.Set();
				};
			};

			Server.Ready += (sender, args) => {
				server_session = args.Session;
			};


			Client.SessionSetup += (sender, args) => {
				var service = new CalculatorService();
				args.Session.AddProxy<ICalculatorService>(service);
			};

			Client.Ready += (sender, e) => {
				var service = e.Session.GetProxy<ICalculatorService>();

				Task.Run(async () => {

					var stream = new RpcStream<SimpleRpcSession, RpcConfig>(e.Session);

					service.UploadStream(stream);

					for (var i = 0; i < 4; i++) {
						await stream.WriteAsync(send_buffer, send_buffer.Length/ 4 * i, send_buffer.Length / 4, CancellationToken.None);
					}

					stream.Close();


				});




			};


			StartAndWait();
		}
	}
}
