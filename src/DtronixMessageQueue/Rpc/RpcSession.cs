using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Amib.Threading;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Socket;
using ProtoBuf;
using ProtoBuf.Meta;

namespace DtronixMessageQueue.Rpc {



	/// <summary>
	/// Session to handle all Rpc call reading/writing for a socket session.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public abstract class RpcSession<TSession, TConfig> : MqSession<TSession, TConfig>, IProcessRpcSession
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
		public SerializationCache SerializationCache { get; private set; }

		/// <summary>
		/// Thread pool for performing tasks on this session.
		/// </summary>
		private SmartThreadPool worker_thread_pool;

		/// <summary>
		/// Contains all outstanding call returns pending a return of data from the recipient connection.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, RpcOperationWait> outstanding_waits =
			new ConcurrentDictionary<ushort, RpcOperationWait>();

		/// <summary>
		/// Contains all operations running on this session which are cancellable.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, RpcOperationWait> ongoing_operations =
			new ConcurrentDictionary<ushort, RpcOperationWait>();

		/// <summary>
		/// Contains all services that can be remotely executed on this session.
		/// </summary>
		private readonly Dictionary<string, IRemoteService<TSession, TConfig>> services =
			new Dictionary<string, IRemoteService<TSession, TConfig>>();

		/// <summary>
		/// Proxy objects to be invoked on this session and proxied to the recipient session.
		/// </summary>
		private readonly Dictionary<Type, IRemoteService<TSession, TConfig>> remote_services_proxy =
			new Dictionary<Type, IRemoteService<TSession, TConfig>>();

		/// <summary>
		///  Base proxy to the used for servicing of the proxy interface.
		/// </summary>
		private readonly Dictionary<Type, RealProxy> remote_service_realproxy = new Dictionary<Type, RealProxy>();

		/// <summary>
		/// Server base socket for this session.
		/// Null if the BaseSocket is not running in server mode.
		/// </summary>
		public RpcServer<TSession, TConfig> Server { get; private set; }

		/// <summary>
		/// Client base socket for this session.
		/// Null if the BaseSocket is not running in client mode.
		/// </summary>
		public RpcClient<TSession, TConfig> Client { get; private set; }

		/// <summary>
		/// Verify the authenticity of the newly connected client.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> Authenticate;

		/// <summary>
		/// Verify the authenticity of the newly connected client.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> AuthenticationResult;

		/// <summary>
		/// True if this session has passed authentication;  False otherwise.
		/// </summary>
		public bool Authenticated { get; set; }

		/// <summary>
		/// Called when this session is being setup.
		/// </summary>
		protected override void OnSetup() {
			base.OnSetup();

			// Determine if this session is running on the server or client to retrieve the worker thread pool.
			if (BaseSocket.Mode == SocketMode.Server) {
				Server = (RpcServer<TSession, TConfig>) BaseSocket;
				worker_thread_pool = Server.WorkerThreadPool;
			} else {
				Client = (RpcClient<TSession, TConfig>) BaseSocket;
				worker_thread_pool = Client.WorkerThreadPool;
			}

			// Create a serialization cache for this session.
			SerializationCache = new SerializationCache(Config);
		}

		protected override void ProcessCommand(MqFrame frame) {
			var command_type = (MqCommandType) frame.ReadByte(0);

			if (command_type != MqCommandType.RpcCommand) {
				base.ProcessCommand(frame);
				return;
			}

			try {
				var rpc_command_type = frame.ReadByte(1);

				if (rpc_command_type == 0) {
					// Welcome message.
					if (BaseSocket.Mode != SocketMode.Client) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					var serializer = SerializationCache.Get(new MqMessage(frame));

					// Forward the reader two bytes to the data.
					serializer.MessageReader.ReadBytes(2);

					serializer.PrepareDeserializeReader();

					// Try to read the information from the server about the server.
					Client.ServerInfo =
						serializer.DeserializeFromReader(typeof(RpcServerInfoDataContract)) as RpcServerInfoDataContract;


					if (Client.ServerInfo.RequireAuthentication) {
						var auth_args = new RpcAuthenticateEventArgs<TSession, TConfig>(this);

						Authenticate?.Invoke(this, auth_args);

						if (auth_args.AuthData != null) {
							
						}

						serializer.MessageWriter.Write((byte)MqCommandType.RpcCommand);
						serializer.MessageWriter.Write((byte)1);

						if (auth_args.AuthData == null) {
							auth_args.AuthData = new byte[] {0};
						}

						serializer.MessageWriter.Write(auth_args.AuthData, 0, auth_args.AuthData.Length);

						var auth_message = serializer.MessageWriter.ToMessage(true);
						auth_message[0].FrameType = MqFrameType.Command;
						
						Send(auth_message);
					}

					SerializationCache.Put(serializer);

				} else if (rpc_command_type == 1) {
					// Authentication request
					if (BaseSocket.Mode != SocketMode.Server) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					if (Server.Config.RequireAuthentication == false) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					byte[] auth_bytes = new byte[frame.DataLength - 2];
					frame.Read(2, auth_bytes, 0, auth_bytes.Length);

					var auth_args = new RpcAuthenticateEventArgs<TSession, TConfig>(this) {
						AuthData = auth_bytes
					};

					
					Authenticate?.Invoke(this, auth_args);

					Authenticated = auth_args.Authenticated;

					var auth_frame = CreateFrame(new byte[auth_args.AuthData.Length + 2], MqFrameType.Command);
					auth_frame.Write(0, (byte)MqCommandType.RpcCommand);
					auth_frame.Write(1, 2);

					// State of the authentication
					auth_frame.Write(2, Authenticated);

					Send(auth_frame);

					if (Authenticated == false) {
						Close(SocketCloseReason.AuthenticationFailure);
					}

				} else if (rpc_command_type == 2) {
					// Authentication result
					if (BaseSocket.Mode != SocketMode.Client) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					if (Client.Config.RequireAuthentication == false) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					Authenticated = true;

					var auth_args = new RpcAuthenticateEventArgs<TSession, TConfig>(this) {
						Authenticated = frame.ReadBoolean(2)
					};

					AuthenticationResult?.Invoke(this, auth_args);

				} else {
					Close(SocketCloseReason.ProtocolError);
				}

			} catch (Exception) {
				Close(SocketCloseReason.ProtocolError);
			}


		}

		/// <summary>
		/// Called when this RpcSession is connected to the socket.
		/// </summary>
		protected override void OnConnected() {

			// If this is a new session on the server, send the welcome message.
			if (BaseSocket.Mode == SocketMode.Server) {
				Server.ServerInfo.RequireAuthentication = Config.RequireAuthentication;

				var serializer = SerializationCache.Get();

				serializer.MessageWriter.Write((byte)MqCommandType.RpcCommand);
				serializer.MessageWriter.Write((byte)0);
				serializer.SerializeToWriter(Server.ServerInfo);

				var message = serializer.MessageWriter.ToMessage(true);

				message[0].FrameType = MqFrameType.Command;

				Send(message);
			}

			base.OnConnected();
		}


		/// <summary>
		/// Event fired when one or more new messages are ready for use.
		/// </summary>
		/// <param name="sender">Originator of call for this event.</param>
		/// <param name="e">Event args for the message.</param>
		protected override void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e) {
			MqMessage message;

			// Continue to parse the messages in this queue.
			while (e.Messages.Count > 0) {
				message = e.Messages.Dequeue();

				// Read the type of message.
				var message_type = (RpcMessageType) message[0].ReadByte(0);

				switch (message_type) {
					case RpcMessageType.Command:
						// Reserved for future use.
						//ProcessRpcCommand(message);
						break;

					case RpcMessageType.RpcCallCancellation:

						// Remotely called to cancel a rpc call on this session.
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
						// Unknown message type passed.  Disconnect the connection.
						e.Session.Close(SocketCloseReason.ProtocolError);
						break;
				}


			}
		}


		/// <summary>
		/// Adds a proxy interface and instance to the current session to allow for remote method proxying.
		/// </summary>
		/// <typeparam name="T">Interface of the instance.  Must be explicitly specified.</typeparam>
		/// <param name="instance">Instance of the interface implementation.</param>
		public void AddProxy<T>(T instance) where T : IRemoteService<TSession, TConfig> {
			var proxy = new RpcProxy<T, TSession, TConfig>(instance, this);
			remote_service_realproxy.Add(typeof(T), proxy);
			remote_services_proxy.Add(typeof(T), (T)proxy.GetTransparentProxy());
		}

		/// <summary>
		/// Returns the proxy of the specified type if it exists on this session.
		/// </summary>
		/// <typeparam name="T">Interface of the proxy to retrieve.</typeparam>
		/// <returns>Proxied interface methods.</returns>
		public T GetProxy<T>() where T : IRemoteService<TSession, TConfig> {
			return (T)remote_services_proxy[typeof(T)];
		}

		/// <summary>
		/// Adds a service to this session to be called remotely.
		/// </summary>
		/// <typeparam name="T">Interface of this type.</typeparam>
		/// <param name="instance">Instance to execute methods on.</param>
		public void AddService<T>(T instance) where T : IRemoteService<TSession, TConfig> {
			services.Add(instance.Name, instance);
			instance.Session = (TSession)this;
		}



		/// <summary>
		/// Processes the incoming Rpc call from the recipient connection.
		/// </summary>
		/// <param name="message">Message containing the Rpc call.</param>
		/// <param name="message_type">Type of call this message is.</param>
		private void ProcessRpcCall(MqMessage message, RpcMessageType message_type) {

			// Execute the processing on the worker thread.
			worker_thread_pool.QueueWorkItem(() => {

				// Retrieve a serialization cache to work with.
				var serialization = SerializationCache.Get(message);
				ushort rec_message_return_id = 0;

				try {
					// Skip RpcMessageType
					serialization.MessageReader.ReadByte();

					// Determine if this call has a return value.
					if (message_type == RpcMessageType.RpcCall) {
						rec_message_return_id = serialization.MessageReader.ReadUInt16();
					}

					// Read the string service name, method and number of arguments.
					var rec_service_name = serialization.MessageReader.ReadString();
					var rec_method_name = serialization.MessageReader.ReadString();
					var rec_argument_count = serialization.MessageReader.ReadByte();

					// Verify that the requested service exists.
					if (services.ContainsKey(rec_service_name) == false) {
						throw new Exception($"Service '{rec_service_name}' does not exist.");
					}

					// Get the service from the instance list.
					var service = services[rec_service_name];

					// Get the actual method.  TODO: Might want to cache this for performance purposes.
					var method_info = service.GetType().GetMethod(rec_method_name);
					var method_parameters = method_info.GetParameters();

					// Determine if the last parameter is a cancellation token.
					var last_param = method_info.GetParameters().LastOrDefault();

					var cancellation_source = new CancellationTokenSource();

					// Number used to increase the number of parameters if there is a cancellation token.
					int cancellation_token_param = 0;
					RpcOperationWait cancellation_wait;

					// If the past parameter is a cancellation token, setup a return wait for this call to allow for remote cancellation.
					if (rec_message_return_id != 0 && last_param?.ParameterType == typeof(CancellationToken)) {
						cancellation_wait = new RpcOperationWait {
							Token = cancellation_source.Token,
							TokenSource = cancellation_source,
							Id = rec_message_return_id
						};

						// Set the number to 1 to increase the parameter number by one.
						cancellation_token_param = 1;

						// Add it to the main list of ongoing operations.
						ongoing_operations.TryAdd(rec_message_return_id, cancellation_wait);
					}

					// Setup the parameters to pass to the invoked method.
					object[] parameters = new object[rec_argument_count + cancellation_token_param];

					// Determine if we have any parameters to pass to the invoked method.
					if (rec_argument_count > 0) {

						serialization.PrepareDeserializeReader();

						// Write all the rest of the message to the stream to parse into parameters.
						/*var param_bytes = serilization.MessageReader.ReadToEnd();
						serilization.Stream.Write(param_bytes, 0, param_bytes.Length);
						serilization.Stream.Position = 0;*/

						// Parse each parameter to the parameter list.
						for (int i = 0; i < rec_argument_count; i++) {
							parameters[i] = serialization.DeserializeFromReader(method_parameters[i].ParameterType, i);
							/*parameters[i] = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(serialization.Stream, null,
								method_parameters[i].ParameterType,
								PrefixStyle.Base128, i);*/
						}
					}
					
					// Add the cancellation token to the parameters.
					if (cancellation_token_param > 0) {
						parameters[parameters.Length - 1] = cancellation_source.Token;
					}


					object return_value;
					try {
						// Invoke the requested method.
						return_value = method_info.Invoke(service, parameters);
					} catch (Exception ex) {
						// Determine if this method was waited on.  If it was and an exception was thrown,
						// Let the recipient session know an exception was thrown.
						if (rec_message_return_id != 0 && ex.InnerException?.GetType() != typeof(OperationCanceledException)) {
							SendRpcException(serialization, ex, rec_message_return_id);
						}
						return;
					} finally {
						// Remove the cancellation wait if it exists.
						ongoing_operations.TryRemove(rec_message_return_id, out cancellation_wait);

					}


					// Determine what to do with the return value.
					if (message_type == RpcMessageType.RpcCall) {
						// Reset the stream.
						serialization.Stream.SetLength(0);

						// Write the Rpc call type and the id.
						serialization.MessageWriter.Clear();
						serialization.MessageWriter.Write((byte) RpcMessageType.RpcCallReturn);
						serialization.MessageWriter.Write(rec_message_return_id);

						// Serialize the return value and add it to the stream.

						serialization.SerializeToWriter(return_value, 0);

						// Send the return value message to the recipient.
						Send(serialization.MessageWriter.ToMessage(true));
					} 

					// Return the serialization to the cache to be reused.
					SerializationCache.Put(serialization);


				} catch (Exception ex) {
					// If an exception occurred, notify the recipient connection.
					SendRpcException(serialization, ex, rec_message_return_id);
					SerializationCache.Put(serialization);
				}
			});

		}



		/// <summary>
		/// Processes the incoming return value message from the recipient connection.
		/// </summary>
		/// <param name="mq_message">Message containing the frames for the return value.</param>
		private void ProcessRpcReturn(MqMessage mq_message) {

			// Execute the processing on the worker thread.
			worker_thread_pool.QueueWorkItem(() => {

				// Retrieve a serialization cache to work with.
				var serialization = SerializationCache.Get(mq_message);
				try {

					// Skip message type byte.
					serialization.MessageReader.ReadByte();

					// Read the return Id.
					var return_id = serialization.MessageReader.ReadUInt16();

					RpcOperationWait call_wait;
					// Try to get the outstanding wait from the return id.  If it does not exist, the has already completed.
					if (outstanding_waits.TryRemove(return_id, out call_wait)) {
						call_wait.ReturnMessage = mq_message;

						// Release the wait event.
						call_wait.ReturnResetEvent.Set();
					}

				} finally {
					SerializationCache.Put(serialization);
				}
			});
		}

		/// <summary>
		/// Takes an exception and serializes the important information and sends it to the recipient connection.
		/// </summary>
		/// <param name="serialization">Serialization information to use for this response.</param>
		/// <param name="ex">Exception which occurred to send to the recipient session.</param>
		/// <param name="message_return_id">Id used to reference this call on the recipient session.</param>
		private void SendRpcException(SerializationCache.Serializer serialization, Exception ex, ushort message_return_id) {
			// Reset the length of the stream to clear it.
			serialization.Stream.SetLength(0);

			// Clear the message writer of any previously stored data.
			serialization.MessageWriter.Clear();

			// Writer the Rpc call type and the return Id.
			serialization.MessageWriter.Write((byte)RpcMessageType.RpcCallException);
			serialization.MessageWriter.Write(message_return_id);

			// Get the exception information in a format that we can serialize.
			var exception = new RpcRemoteExceptionDataContract(ex is TargetInvocationException ? ex.InnerException : ex);

			// Serialize the class with a length prefix.
			serialization.SerializeToWriter(exception, 0);

			// Send the message to the recipient connection.
			Send(serialization.MessageWriter.ToMessage(true));

		}

		/// <summary>
		/// Creates a waiting operation for this session.  Could be a remote cancellation request or a pending result request.
		/// </summary>
		/// <returns>Wait operation to wait on.</returns>
		RpcOperationWait IProcessRpcSession.CreateWaitOperation() {
			var return_wait = new RpcOperationWait {
				ReturnResetEvent = new ManualResetEventSlim()
			};

			// Lock the id incrementation to prevent duplicates.
			lock (rpc_call_id_lock) {
				if (++rpc_call_id > ushort.MaxValue) {
					rpc_call_id = 0;
				}
				return_wait.Id = (ushort)rpc_call_id;
			}

			// Add the wait to the outstanding wait dictionary for retrieval later.
			if (outstanding_waits.TryAdd(return_wait.Id, return_wait) == false) {
				throw new InvalidOperationException($"Id {return_wait.Id} already exists in the return_wait_handles dictionary.");
			}

			return return_wait;
		}

		/// <summary>
		/// Called to cancel a remote waiting operation on the recipient connection.
		/// </summary>
		/// <param name="id">Id of the waiting operation to cancel.</param>
		void IProcessRpcSession.CancelWaitOperation(ushort id) {
			RpcOperationWait call_wait;

			// Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
			if (outstanding_waits.TryRemove(id, out call_wait)) {
				var frame = new MqFrame(new byte[3], MqFrameType.Last, Config);
				frame.Write(0, (byte) RpcMessageType.RpcCallCancellation);
				frame.Write(1, id);

				Send(frame);
			}
		}

	}
}
