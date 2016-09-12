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

			var store = session.Store.Get();

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
				store.MessageWriter.Write((byte)RpcMessageType.RpcCallNoReturn);
			} else {
				store.MessageWriter.Write((byte)RpcMessageType.RpcCall);

				return_wait = session.CreateReturnCallWait();
				store.MessageWriter.Write(return_wait.Id);
				return_wait.Token = cancellation_token;
			}

			store.MessageWriter.Write(decorated.Name);
			store.MessageWriter.Write(method_call.MethodName);
			store.MessageWriter.Write((byte)arguments.Length);

			int field_number = 0;
			foreach (var arg in arguments) {
				RuntimeTypeModel.Default.SerializeWithLengthPrefix(store.Stream, arg, arg.GetType(), PrefixStyle.Base128, field_number++);

				store.MessageWriter.Write(store.Stream.ToArray());
				// Should always read the entire buffer in one go.

				store.Stream.SetLength(0);
			}

			session.Send(store.MessageWriter.ToMessage(true));

			// If there is no return wait, our work on this session is complete.
			if (return_wait == null) {
				return new ReturnMessage(null, null, 0, method_call.LogicalCallContext, method_call);
			}

			return_wait.ReturnResetEvent.Wait(return_wait.Token);

			return_wait.Token.ThrowIfCancellationRequested();

			try {
				store.MessageReader.Message = return_wait.ReturnMessage;
				
				var return_type = (RpcMessageType)store.MessageReader.ReadByte();
				// Skip 2 bytes for the return ID
				store.MessageReader.ReadBytes(2);

				var return_bytes = store.MessageReader.ReadToEnd();
				store.Stream.SetLength(0);
				store.Stream.Write(return_bytes, 0, return_bytes.Length);
				store.Stream.Position = 0;

				switch (return_type) {
					case RpcMessageType.RpcCallReturn:
						var return_value = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(store.Stream, null, method_info.ReturnType, PrefixStyle.Base128, 0);
						return new ReturnMessage(return_value, null, 0, method_call.LogicalCallContext, method_call);

					case RpcMessageType.RpcCallException:
						var return_exception = (RpcRemoteExceptionDataContract)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(store.Stream, null, typeof(RpcRemoteExceptionDataContract), PrefixStyle.Base128, 0);
						return new ReturnMessage(new RpcRemoteException(return_exception), method_call);

					default:
						throw new ArgumentOutOfRangeException();
				}

			} finally {
				session.Store.Put(store);
			}
		}
	}
}
