using System.Threading;

namespace DtronixMessageQueue.Rpc {
	public class RpcReturnCallWait {
		public ushort Id { get; set; }
		public ManualResetEventSlim ReturnResetEvent { get; set; }
		public MqMessage ReturnMessage { get; set; }
		public CancellationToken Token { get; set; }
	}
}
