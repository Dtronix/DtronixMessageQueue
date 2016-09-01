using System.Net.Sockets;

namespace DtronixMessageQueue.Rpc {
	public class RpcServer<TSession> : MqServer<TSession>
		where TSession : RpcSession<TSession>, new() {

		public RpcServer(MqSocketConfig config) : base(config) {

		}

		protected override TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = base.CreateSession(socket);
			session.Server = this;
			return session;
		}


	}
}
