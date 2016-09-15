using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Amib.Threading;
using DtronixMessageQueue.Socket;
using ProtoBuf;
using ProtoBuf.Meta;

namespace DtronixMessageQueue.Rpc {



	/// <summary>
	/// Session to handle all Rpc call reading/writing for a socket session.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class RpcSession<TSession, TConfig> : MqSession<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

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
		/// Thread pool for performing tasks on this session.
		/// </summary>
		private SmartThreadPool worker_thread_pool;

		/// <summary>
		/// Contains all outstanding call returns pending a return of data from the other end of the connection.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, RpcOperationWait> outstanding_waits = new ConcurrentDictionary<ushort, RpcOperationWait>();

		/// <summary>
		/// Contains all operations running on this session which are cancellable.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, RpcOperationWait> ongoing_operations = new ConcurrentDictionary<ushort, RpcOperationWait>();

		private readonly Dictionary<string, IRemoteService<TSession, TConfig>> services = new Dictionary<string, IRemoteService<TSession, TConfig>>();
		private readonly Dictionary<Type, IRemoteService<TSession, TConfig>> remote_services_proxy = new Dictionary<Type, IRemoteService<TSession, TConfig>>();
		private readonly Dictionary<Type, RealProxy> remote_service_realproxy = new Dictionary<Type, RealProxy>();


		public RpcServer<TSession, TConfig> Server { get; set; }

		protected override void OnSetup() {
			base.OnSetup();

			var config = (MqConfig) Config;

			if (Server != null) {
				worker_thread_pool = Server.WorkerThreadPool;
			} else {
				worker_thread_pool = new SmartThreadPool(config.IdleWorkerTimeout, config.MaxReadWriteWorkers, 1);
			}

			Store = new SerializationStore(config);
		}


		public override void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e) {
			MqMessage message;
			while (e.Messages.Count > 0) {
				message = e.Messages.Dequeue();

				// Read the type of message.
				var message_type = (RpcMessageType) message[0].ReadByte(0);

				switch (message_type) {
					case RpcMessageType.Command:
						ProcessRpcCommand(message, e);
						break;

					case RpcMessageType.RpcCallCancellation:
						var cancellation_id = message[0].ReadUInt16(1);
						RpcOperationWait wait_operation;
						if (ongoing_operations.TryRemove(cancellation_id, out wait_operation)) {
							wait_operation.TokenSource.Cancel();
						}
						break;

					case RpcMessageType.RpcCallNoReturn:
					case RpcMessageType.RpcCall:
						ProcessRpcCall(message, message_type);
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


		public void AddProxy<T>(T instance) where T : IRemoteService<TSession, TConfig> {
			var proxy = new RpcProxy<T, TSession, TConfig>(instance, this);
			remote_service_realproxy.Add(typeof(T), proxy);
			remote_services_proxy.Add(typeof(T), (T)proxy.GetTransparentProxy());
		}

		public T GetProxy<T>() where T : IRemoteService<TSession, TConfig> {
			return (T)remote_services_proxy[typeof(T)];
		}

		public void AddService<T>(T instance) where T : IRemoteService<TSession, TConfig> {
			services.Add(instance.Name, instance);
			instance.Session = (TSession)this;
		}




		private void ProcessRpcCall(MqMessage message, RpcMessageType message_type) {

			worker_thread_pool.QueueWorkItem(() => {
				var store = Store.Get();
				ushort rec_message_return_id = 0;
				try {
					store.MessageReader.Message = message;

					// Skip RpcMessageType
					store.MessageReader.ReadByte();

					if (message_type == RpcMessageType.RpcCall) {
						rec_message_return_id = store.MessageReader.ReadUInt16();
					}

					var rec_service_name = store.MessageReader.ReadString();
					var rec_method_name = store.MessageReader.ReadString();
					var rec_argument_count = store.MessageReader.ReadByte();

					if (services.ContainsKey(rec_service_name) == false) {
						throw new Exception($"Service '{rec_service_name}' does not exist.");
					}

					var service = services[rec_service_name];

					var method_info = service.GetType().GetMethod(rec_method_name);
					var method_parameters = method_info.GetParameters();

					


					var last_param = method_info.GetParameters().LastOrDefault();

					var cancellation_source = new CancellationTokenSource();
					int cancellation_token_param = 0;
					if (rec_message_return_id != 0 && last_param?.ParameterType == typeof(CancellationToken)) {
						var return_wait = new RpcOperationWait {
							Token = cancellation_source.Token,
							TokenSource = cancellation_source,
							Id = rec_message_return_id
						};
						cancellation_token_param = 1;
						ongoing_operations.TryAdd(rec_message_return_id, return_wait);
					}

					object[] parameters = new object[rec_argument_count + cancellation_token_param];


					if (rec_argument_count > 0) {
						// Write all the rest of the message to the stream to parse into parameters.
						var param_bytes = store.MessageReader.ReadToEnd();
						store.Stream.Write(param_bytes, 0, param_bytes.Length);
						store.Stream.Position = 0;

						for (int i = 0; i < rec_argument_count; i++) {
							parameters[i] = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(store.Stream, null,
								method_parameters[i].ParameterType,
								PrefixStyle.Base128, i);
						}
					}

					if (cancellation_token_param > 0) {
						parameters[parameters.Length - 1] = cancellation_source.Token;
					}


					object return_value;
					try {
						return_value = method_info.Invoke(service, parameters);
					} catch (Exception ex) {
						if (rec_message_return_id != 0 && ex.InnerException?.GetType() != typeof(OperationCanceledException)) {
							SendRpcException(store, ex, rec_message_return_id);
						}
						return;
					}



					switch (message_type) {
						case RpcMessageType.RpcCall:
							store.Stream.SetLength(0);

							store.MessageWriter.Clear();
							store.MessageWriter.Write((byte) RpcMessageType.RpcCallReturn);
							store.MessageWriter.Write(rec_message_return_id);

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
					SendRpcException(store, ex, rec_message_return_id);
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

			RuntimeTypeModel.Default.SerializeWithLengthPrefix(store.Stream, exception, exception.GetType(), PrefixStyle.Base128, 0);

			store.MessageWriter.Write(store.Stream.ToArray());

			Send(store.MessageWriter.ToMessage(true));

		}

		public RpcOperationWait CreateWaitOperation() {
			var return_wait = new RpcOperationWait {
				ReturnResetEvent = new ManualResetEventSlim()
			};

			lock (rpc_call_id_lock) {
				if (++rpc_call_id > ushort.MaxValue) {
					rpc_call_id = 0;
				}
				return_wait.Id = (ushort)rpc_call_id;
			}

			if (outstanding_waits.TryAdd(return_wait.Id, return_wait) == false) {
				throw new InvalidOperationException($"Id {return_wait.Id} already exists in the return_wait_handles dictionary.");
			}

			return return_wait;
		}

		public void CancelWaitOperation(ushort id) {
			RpcOperationWait call_wait;
			outstanding_waits.TryRemove(id, out call_wait);

			var frame = new MqFrame(new byte[3], MqFrameType.Last, (MqConfig) Config);
			frame.Write(0, (byte)RpcMessageType.RpcCallCancellation);
			frame.Write(1, id);

			Send(frame);
		}


		private void ProcessRpcReturn(MqMessage mq_message) {
			var store = Store.Get();
			try {
				store.MessageReader.Message = mq_message;

				// Skip message type byte.
				store.MessageReader.ReadByte();

				var return_id = store.MessageReader.ReadUInt16();
				RpcOperationWait call_wait;
				if (outstanding_waits.TryRemove(return_id, out call_wait) == false) {
					return;
				}

				call_wait.ReturnMessage = mq_message;

				call_wait.ReturnResetEvent.Set();
			} finally {
				Store.Put(store);
			}
		}


		private void ProcessRpcCommand(MqMessage mq_message, IncomingMessageEventArgs<TSession, TConfig> incoming_message_event_args) {
			throw new NotImplementedException();
		}
	}
}
