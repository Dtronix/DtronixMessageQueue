using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using DtronixMessageQueue.Socket;
using Newtonsoft.Json.Linq;

namespace DtronixMessageQueue.Rpc {
	public class RpcSession<TSession> : MqSession<TSession>
		where TSession : RpcSession<TSession>, new() {


		private int rpc_call_id;

		private object rpc_call_id_lock = new object();

		public BsonReadWriteStore ReadWriteStore { get; private set; }

		private ConcurrentDictionary<ushort, RpcReturnCallWait> return_call_wait = 
			new ConcurrentDictionary<ushort, RpcReturnCallWait>();

		private Dictionary<string, IRemoteService<TSession>> services = new Dictionary<string, IRemoteService<TSession>>();
		private Dictionary<Type, IRemoteService<TSession>> remote_services_proxy = new Dictionary<Type, IRemoteService<TSession>>();
		private Dictionary<Type, RealProxy> remote_service_realproxy = new Dictionary<Type, RealProxy>();


		public RpcServer<TSession> Server { get; set; }

		protected override void OnSetup() {
			base.OnSetup();

			ReadWriteStore = new BsonReadWriteStore((MqSocketConfig)Config);
		}


		public override void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession> e) {
			MqMessage message;
			while (e.Messages.Count > 0) {
				message = e.Messages.Dequeue();

				// Read the type of message.
				var message_type = (RpcMessageType) message[0].ReadByte(0);

				//var message_type = Enum ;

				switch (message_type) {
					case RpcMessageType.Command:
						ProcessRpcCommand(message, e);
						break;

					case RpcMessageType.RpcCallNoReturn:
					case RpcMessageType.RpcCall:
						ProcessRpcCall(message, e, message_type);
						break;

					case RpcMessageType.RpcCallException:
					case RpcMessageType.RpcCallReturn:
						ProcessRpcReturn(message);
						break;

					default:
						e.Session.Close(SocketCloseReason.ProtocolError);
						break;
				}
			}
		}


		public void AddProxy<T>(T instance) where T : IRemoteService<TSession> {
			var proxy = new RpcProxy<T, TSession>(instance, this);
			remote_service_realproxy.Add(typeof(T), proxy);
			remote_services_proxy.Add(typeof(T), (T)proxy.GetTransparentProxy());
		}

		public T GetProxy<T>() where T : IRemoteService<TSession> {
			return (T)remote_services_proxy[typeof(T)];
		}

		public void AddService<T>(T instance) where T : IRemoteService<TSession>{
			services.Add(instance.Name, instance);
			instance.Session = (TSession)this;
		}




		private void ProcessRpcCall(MqMessage message, IncomingMessageEventArgs<TSession> e, RpcMessageType message_type) {
			var store = ReadWriteStore.Get();
			ushort message_return_id = 0;
			try {
				store.MessageReader.Message = message;

				// Skip RpcMessageType
				store.MessageReader.ReadByte();

				if (message_type == RpcMessageType.RpcCall) {
					message_return_id = store.MessageReader.ReadUInt16();
				}

				var service_name = store.MessageReader.ReadString();
				var method_name = store.MessageReader.ReadString();
				var argument_count = store.MessageReader.ReadByte();

				if (services.ContainsKey(service_name) == false) {
					throw new RpcRemoteException($"Service '{service_name}' does not exist.");
				}

				var service = services[service_name];

				var method_info = service.GetType().GetMethod(method_name);
				var method_parameters = method_info.GetParameters();

				object[] parameters = new object[argument_count];

				// Deserialize each parameter.
				JObject param_jobject = (JObject)store.Serializer.Deserialize(store.BsonReader);
				var param_children = param_jobject.PropertyValues().ToArray();

				for (int i = 0; i < argument_count; i++) {
					parameters[i] = param_children[i].ToObject(method_parameters[i].ParameterType);
				}

				var return_value = method_info.Invoke(service, parameters);


				switch (message_type) {
					case RpcMessageType.RpcCall:

						store.MessageWriter.Clear();
						store.MessageWriter.Write((byte)RpcMessageType.RpcCallReturn);
						store.MessageWriter.Write(message_return_id);
						store.Serializer.Serialize(store.BsonWriter, new[] {return_value});

						Send(store.MessageWriter.ToMessage(true));

						break;
					case RpcMessageType.RpcCallNoReturn:
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(message_type), message_type, null);
				}

			} catch (Exception ex) {
				store.MessageWriter.Clear();
				store.MessageWriter.Write((byte)RpcMessageType.RpcCallException);
				store.MessageWriter.Write(message_return_id);

				if (ex is RpcRemoteException == false) {
					ex = new RpcRemoteException("Remote call threw an exception", ex);
				}

				store.Serializer.Serialize(store.BsonWriter, ex);

				Send(store.MessageWriter.ToMessage(true));

			} finally {
				ReadWriteStore.Put(store);
			}

		}

		public RpcReturnCallWait CreateReturnCallWait() {
			var return_wait = new RpcReturnCallWait {
				ReturnResetEvent = new ManualResetEventSlim()
			};

			lock (rpc_call_id_lock) {
				if (++rpc_call_id > ushort.MaxValue) {
					rpc_call_id = 0;
				}
				return_wait.Id = (ushort)rpc_call_id;
			}

			if (return_call_wait.TryAdd(return_wait.Id, return_wait) == false) {
				throw new InvalidOperationException($"Id {return_wait.Id} already exists in the return_wait_handles dictionary.");
			}

			return return_wait;
		}


		private void ProcessRpcReturn(MqMessage mq_message) {
			var store = ReadWriteStore.Get();
			try {
				store.MessageReader.Message = mq_message;

				// Skip message type byte.
				store.MessageReader.ReadByte();

				var return_id = store.MessageReader.ReadUInt16();
				RpcReturnCallWait call_wait;
				if (return_call_wait.TryRemove(return_id, out call_wait) == false) {
					return;
				}

				call_wait.ReturnMessage = mq_message;

				call_wait.ReturnResetEvent.Set();
			} finally {
				ReadWriteStore.Put(store);
			}
		}


		private void ProcessRpcCommand(MqMessage mq_message, IncomingMessageEventArgs<TSession> incoming_message_event_args) {
			throw new NotImplementedException();
		}
	}
}
