using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using ProtoBuf;
using ProtoBuf.Meta;

namespace DtronixMessageQueue.Rpc {
	class RpcProxy<T, TSession> : RealProxy
		where T : IRemoteService<TSession>
		where TSession : RpcSession<TSession>, new() {

		private readonly T decorated;
		private readonly TSession session;

		public RpcProxy(T decorated, RpcSession<TSession> session) : base(typeof(T)) {
			this.decorated = decorated;
			this.session = (TSession)session;
		}

		public override IMessage Invoke(IMessage msg) {
			var method_call = msg as IMethodCallMessage;
			var method_info = method_call.MethodBase as MethodInfo;

			var rw_store = session.ReadWriteStore.Get();

			object[] arguments = method_call.Args;
			CancellationToken cancellation_token = CancellationToken.None;

			if (method_call.ArgCount > 0) {
				var last_argument = method_call.Args.Last();

				if (last_argument is CancellationToken) {
					cancellation_token = (CancellationToken) last_argument;

					if (method_call.ArgCount > 1) {
						arguments = method_call.Args.Take(method_call.ArgCount - 1).ToArray();
					}
				}
			}


			RpcReturnCallWait return_wait = null;

			// Determine what kind of method we are calling.
			if (method_info.ReturnType == typeof(void)) {
				rw_store.MessageWriter.Write((byte)RpcMessageType.RpcCallNoReturn);
			} else {
				rw_store.MessageWriter.Write((byte)RpcMessageType.RpcCall);

				return_wait = session.CreateReturnCallWait();
				rw_store.MessageWriter.Write(return_wait.Id);
			}

			rw_store.MessageWriter.Write(decorated.Name);
			rw_store.MessageWriter.Write(method_call.MethodName);
			rw_store.MessageWriter.Write((byte)arguments.Length);

			foreach (var arg in arguments) {
				RuntimeTypeModel.Default.SerializeWithLengthPrefix(rw_store.Stream, arg, arg.GetType(), PrefixStyle.Fixed32, );

				rw_store.MessageWriter.Write((uint)rw_store.Stream.Length);
				rw_store.MessageWriter.Write(rw_store.Stream.ToArray());
				// Should always read the entire buffer in one go.

				rw_store.Stream.SetLength(0);
			}

			session.Send(rw_store.MessageWriter.ToMessage(true));

			if (return_wait == null) {
				return new ReturnMessage(null, null, 0, method_call.LogicalCallContext, method_call);
			}

			return_wait.ReturnResetEvent.Wait(return_wait.Token);


			try {
				rw_store.MessageReader.Message = return_wait.ReturnMessage;
				
				var return_type = (RpcMessageType)rw_store.MessageReader.ReadByte();
				// Skip 2 bytes.
				rw_store.MessageReader.ReadBytes(2);
				object return_value = typeof(void);
				rw_store.MessageReader.ReadBytes()
				stream.Write();



				RuntimeTypeModel.Default.Deserialize()

				JObject return_jobject = (JObject) rw_store.Serializer.Deserialize(rw_store.BsonReader);
				var return_children = return_jobject.PropertyValues().ToArray();

				switch (return_type) {
					case RpcMessageType.RpcCallReturn:
						return_value = return_children[0].ToObject(method_info.ReturnType);
						return new ReturnMessage(return_value, null, 0, method_call.LogicalCallContext, method_call);

					case RpcMessageType.RpcCallException:
						var exception = (RpcRemoteException)return_children[0].ToObject(typeof(RpcRemoteException));

						return new ReturnMessage(new Exception(exception.Message), method_call);

					default:
						throw new ArgumentOutOfRangeException();
				}

			} finally {
				session.ReadWriteStore.Put(rw_store);
			}
		}
	}
}
