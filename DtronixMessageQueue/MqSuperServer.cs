using System;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	internal sealed class MqSuperServer : AppServer<MqSession, RequestInfo<byte, byte[]>> {
		public MqSuperServer()
			: base(new MqReceiveFilterFactory()) {
		}

		private void OnNewRequestReceived(MqSession session, BinaryRequestInfo request_info) {
			throw new NotImplementedException();
		}
	}
}