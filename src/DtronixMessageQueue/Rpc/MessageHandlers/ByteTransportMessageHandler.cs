using System.Collections.Concurrent;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc.MessageHandlers {
	public class ByteTransportMessageHandler<TSession, TConfig> : MessageHandler<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Current call Id wich gets incremented for each call return request.
		/// </summary>
		private int stream_id;

		/// <summary>
		/// Lock to increment and loop return ID.
		/// </summary>
		private readonly object stream_id_lock = new object();

		/// <summary>
		/// Id byte which precedes all messages all messages of this type.
		/// </summary>
		public override byte Id => 2;

		/// <summary>
		/// Contains all local transports.
		/// </summary>
		public readonly ConcurrentDictionary<ushort, RpcWaitHandle> LocalTransports =
			new ConcurrentDictionary<ushort, RpcWaitHandle>();

		/// <summary>
		/// Contains all remote transports.
		/// </summary>
		public readonly ConcurrentDictionary<ushort, RpcWaitHandle> RemoteTransports =
			new ConcurrentDictionary<ushort, RpcWaitHandle>();


		public ByteTransportMessageHandler(TSession session) : base(session) {
		}

		

		public override bool HandleMessage(MqMessage message) {
			if (message[0][0] != Id) {
				return false;
			}

			// Read the type of message.
			var message_type = (ByteTransportMessageType)message[0].ReadByte(1);

			switch (message_type) {

				case ByteTransportMessageType.RequestTransportHandle:

					// Remotely called to cancel a rpc call on this session.
					var cancellation_id = message[0].ReadUInt16(2);
					RpcWaitHandle wait_handle;
					if (RemoteWaitHandles.TryRemove(cancellation_id, out wait_handle)) {
						wait_handle.TokenSource.Cancel();
					}
					break;

				default:
					// Unknown message type passed.  Disconnect the connection.
					Session.Close(SocketCloseReason.ProtocolError);
					break;
			}

			return true;

		}
	}
}
