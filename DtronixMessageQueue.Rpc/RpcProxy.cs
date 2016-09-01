using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Newtonsoft.Json.Linq;

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
				rw_store.MessageWriter.Write((byte) RpcMessageType.RpcCall);

				return_wait = session.CreateReturnCallWait();
				rw_store.MessageWriter.Write(return_wait.Id);
			}

			rw_store.MessageWriter.Write(decorated.Name);
			rw_store.MessageWriter.Write(method_call.MethodName);
			rw_store.MessageWriter.Write((byte)arguments.Length);

			if (arguments != null) {
				rw_store.Serializer.Serialize(rw_store.BsonWriter, arguments);
			}

			session.Send(rw_store.MessageWriter.ToMessage(true));

			return_wait?.ReturnResetEvent.Wait(return_wait.Token);

			try {
				rw_store.MessageReader.Message = return_wait.ReturnMessage;

				// Skip 3 bytes.
				rw_store.MessageReader.ReadBytes(3);
				JObject return_jobject = (JObject)rw_store.Serializer.Deserialize(rw_store.BsonReader);
				var param_children = return_jobject.PropertyValues().ToArray();
				var return_value = param_children[0].ToObject(method_info.ReturnType);

				return new ReturnMessage(return_value, null, 0, method_call.LogicalCallContext, method_call);

				/*switch (return_msg_type) {
					case RpcMessageType.RpcCallReturn:

					case RpcMessageType.RpcCallException:
						var exception_type_string = rw_store.MessageReader.ReadString();
						var exception_instance = (Exception)rw_store.Serializer.Deserialize(rw_store.BsonReader, Type.GetType(exception_type_string, true));
						var exception = new RpcRemoteException($"Method: {method_info.Name} threw an exception.", exception_instance);

						return new ReturnMessage(exception, method_call);
				}

				return new ReturnMessage(new InvalidOperationException("Session returned malformed request."), method_call);*/
			} finally {
				session.ReadWriteStore.Put(rw_store);
			}

			
		}


	}
}
