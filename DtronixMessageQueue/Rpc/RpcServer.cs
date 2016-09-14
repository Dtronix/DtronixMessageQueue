using System.Net.Sockets;
using Amib.Threading;

namespace DtronixMessageQueue.Rpc {
	public class RpcServer<TSession> : MqServer<TSession>
		where TSession : RpcSession<TSession>, new() {

		public SmartThreadPool WorkerThreadPool { get; }

		public RpcServer(MqSocketConfig config) : base(config) {
			WorkerThreadPool = new SmartThreadPool(config.IdleWorkerTimeout, config.MaxReadWriteWorkers, 1);
		}

		protected override TSession CreateSession(System.Net.Sockets.Socket socket) {
			var session = base.CreateSession(socket);
			session.Server = this;
			return session;
		}


	}
}
