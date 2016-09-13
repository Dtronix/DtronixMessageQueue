using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Socket;
using ProtoBuf;
using ProtoBuf.Meta;

namespace DtronixMessageQueue.Rpc {
	public class RpcSession<TSession> : MqSession<TSession>
		where TSession : RpcSession<TSession>, new() {

		/// <summary>
		/// Current call Id wich gets incremented for each call return request.
		/// </summary>
		private int rpc_call_id;

		/// <summary>
		/// Lock to increment and loop return ID.
		/// </summary>
		private readonly object rpc_call_id_lock = new object();

		/// <summary>
		/// Store which contains instances of all classes for serialization and destabilization of data.
		/// </summary>
		public SerializationStore Store { get; private set; }

		/// <summary>
		/// Contains all outstanding call returns pending a return of data from the other end of the connection.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, RpcReturnCallWait> return_call_wait = new ConcurrentDictionary<ushort, RpcReturnCallWait>();

		private readonly Dictionary<string, IRemoteService<TSession>> services = new Dictionary<string, IRemoteService<TSession>>();
		private readonly Dictionary<Type, IRemoteService<TSession>> remote_services_proxy = new Dictionary<Type, IRemoteService<TSession>>();
		private readonly Dictionary<Type, RealProxy> remote_service_realproxy = new Dictionary<Type, RealProxy>();


		public RpcServer<TSession> Server { get; set; }

		protected override void OnSetup() {
			base.OnSetup();

			Store = new SerializationStore((MqSocketConfig)Config);
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
			Task.Run(() => {
				var store = Store.Get();
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
						throw new Exception($"Service '{service_name}' does not exist.");
					}

					var service = services[service_name];

					var method_info = service.GetType().GetMethod(method_name);
					var method_parameters = method_info.GetParameters();

					object[] parameters = new object[argument_count];

					if (argument_count > 0) {
						// Write all the rest of the message to the stream to parse into parameters.
						var param_bytes = store.MessageReader.ReadToEnd();
						store.Stream.Write(param_bytes, 0, param_bytes.Length);
						store.Stream.Position = 0;

						for (int i = 0; i < argument_count; i++) {
							parameters[i] = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(store.Stream, null,
								method_parameters[i].ParameterType,
								PrefixStyle.Base128, i);
						}
					}
					object return_value;
					try {
						return_value = method_info.Invoke(service, parameters);
					} catch (Exception ex) {
						SendRpcException(store, ex, message_return_id);
						return;
					}



					switch (message_type) {
						case RpcMessageType.RpcCall:
							store.Stream.SetLength(0);

							store.MessageWriter.Clear();
							store.MessageWriter.Write((byte) RpcMessageType.RpcCallReturn);
							store.MessageWriter.Write(message_return_id);

							RuntimeTypeModel.Default.SerializeWithLengthPrefix(store.Stream, return_value, return_value.GetType(),
								PrefixStyle.Base128, 0);

							store.MessageWriter.Write(store.Stream.ToArray());

							Send(store.MessageWriter.ToMessage(true));

							break;
						case RpcMessageType.RpcCallNoReturn:
							break;
					}

					Store.Put(store);


				} catch (Exception ex) {
					SendRpcException(store, ex, message_return_id);
					Store.Put(store);
				}
			});

		}

		private void SendRpcException(SerializationStore.Store store, Exception ex, ushort message_return_id) {
			store.Stream.SetLength(0);

			store.MessageWriter.Clear();
			store.MessageWriter.Write((byte)RpcMessageType.RpcCallException);
			store.MessageWriter.Write(message_return_id);

			var exception = new RpcRemoteExceptionDataContract(ex is TargetInvocationException ? ex.InnerException : ex);

			RuntimeTypeModel.Default.SerializeWithLengthPrefix(store.Stream, exception, exception.GetType(), PrefixStyle.Base128, 1);

			store.MessageWriter.Write(store.Stream.ToArray());

			Send(store.MessageWriter.ToMessage(true));

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
			var store = Store.Get();
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
				Store.Put(store);
			}
		}


		private void ProcessRpcCommand(MqMessage mq_message, IncomingMessageEventArgs<TSession> incoming_message_event_args) {
			throw new NotImplementedException();
		}
	}
}
