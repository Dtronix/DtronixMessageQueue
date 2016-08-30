using System.Net;
using System.Net.Sockets;

namespace DtronixMessageQueue.Socket {
	public class SocketClient<TSession> : SocketBase<TSession>
		where TSession : SocketSession, new() {

		public TSession Session { get; private set; }

		public SocketClient(SocketConfig config) : base(config) {
		}

		public void Connect() {
			Connect(new IPEndPoint(IPAddress.Parse(Config.Ip), Config.Port));
		}

		public void Connect(IPEndPoint end_point) {
			MainSocket = new System.Net.Sockets.Socket(end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
				NoDelay = true
			};

			var event_arg = new SocketAsyncEventArgs {
				RemoteEndPoint = end_point
			};

			event_arg.Completed += (sender, args) => {
				if (args.LastOperation == SocketAsyncOperation.Connect) {
					Session = CreateSession(MainSocket);
					OnConnect(Session);
				}
			};

			MainSocket.ConnectAsync(event_arg);
		}


		/// <summary>
		/// Creates a frame with the specified bytes and the current configurations.
		/// </summary>
		/// <param name="bytes">Bytes to put in the frame.</param>
		/// <returns>Configured frame.</returns>
		public override MqFrame CreateFrame(byte[] bytes) {
			return Utilities.CreateFrame(bytes, MqFrameType.Unset, (MqSocketConfig) Config);
		}

		/// <summary>
		/// Creates a frame with the specified bytes and the current configurations.
		/// </summary>
		/// <param name="bytes">Bytes to put in the frame.</param>
		/// <param name="type">Type of frame to create.</param>
		/// <returns>Configured frame.</returns>
		public override MqFrame CreateFrame(byte[] bytes, MqFrameType type) {
			return Utilities.CreateFrame(bytes, type, (MqSocketConfig)Config);
		}
	}
}
