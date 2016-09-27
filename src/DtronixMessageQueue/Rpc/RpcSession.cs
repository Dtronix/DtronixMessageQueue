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

namespace DtronixMessageQueue.Rpc {



	/// <summary>
	/// Session to handle all Rpc call reading/writing for a socket session.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public abstract class RpcSession<TSession, TConfig> : MqSession<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		public Dictionary<byte, MessageHandler<TSession, TConfig>> MessageHandlers { get; }

		protected RpcCallMessageHandler<TSession, TConfig> RpcCallHandler;
		/// <summary>
		/// Store which contains instances of all classes for serialization and destabilization of data.
		/// </summary>
		public SerializationCache SerializationCache { get; set; }

		/// <summary>
		/// Thread pool for performing tasks on this session.
		/// </summary>
		IWorkItemsGroup WorkerThreadPool { get; set; }

		/// <summary>
		/// Contains all active stream handles for this session.
		/// </summary>
		private readonly ConcurrentDictionary<ushort, RpcWaitHandle> stream_handles =
			new ConcurrentDictionary<ushort, RpcWaitHandle>();

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
		/// Event invoked once when the RpcSession has been authenticated and is ready for usage.
		/// </summary>
		public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;

		/// <summary>
		/// True if this session has passed authentication;  False otherwise.
		/// </summary>
		public bool Authenticated { get; private set; }

		protected RpcSession() {
			MessageHandlers = new Dictionary<byte, MessageHandler<TSession, TConfig>>();
		}

		/// <summary>
		/// Called when this session is being setup.
		/// </summary>
		protected override void OnSetup() {
			base.OnSetup();

			// Determine if this session is running on the server or client to retrieve the worker thread pool.
			if (BaseSocket.Mode == SocketMode.Server) {
				Server = (RpcServer<TSession, TConfig>) BaseSocket;
				WorkerThreadPool = Server.WorkerThreadPool.CreateWorkItemsGroup(Config.MaxSessionConcurrency);

			} else {
				Client = (RpcClient<TSession, TConfig>) BaseSocket;
				WorkerThreadPool = Client.WorkerThreadPool.CreateWorkItemsGroup(Config.MaxSessionConcurrency);
			}

			SerializationCache = new SerializationCache(Config);

			RpcCallHandler = new RpcCallMessageHandler<TSession, TConfig>((TSession)this, WorkerThreadPool);

			MessageHandlers.Add(RpcCallHandler.Id, RpcCallHandler);

		}

		/// <summary>
		/// Processes an incoming command frame from the connection.
		/// Captures the call if it is a Rpc command.
		/// </summary>
		/// <param name="frame">Command frame to process.</param>
		protected override void ProcessCommand(MqFrame frame) {
			var command_type = (MqCommandType) frame.ReadByte(0);

			// If this is a base MqCommand, pass this directly on to the base command handler.
			if (command_type != MqCommandType.RpcCommand) {
				base.ProcessCommand(frame);
				return;
			}

			try {
				var rpc_command_type = (RpcCommandType) frame.ReadByte(1);

				if (rpc_command_type == RpcCommandType.WelcomeMessage) {
					// RpcCommand:byte; RpcCommandType:byte; RpcServerInfoDataContract:byte[];

					// Ensure that this command is running on the client.
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

					if (Client.ServerInfo == null) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					// Check to see if the server requires authentication.  If so, send a auth check.
					if (Client.ServerInfo.RequireAuthentication) {
						var auth_args = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this);

						// Start the authentication event to get the auth data.
						Authenticate?.Invoke(this, auth_args);

						serializer.MessageWriter.Write((byte) MqCommandType.RpcCommand);
						serializer.MessageWriter.Write((byte) RpcCommandType.AuthenticationRequest);

						if (auth_args.AuthData == null) {
							auth_args.AuthData = new byte[] {0};
						}


						serializer.MessageWriter.Write(auth_args.AuthData, 0, auth_args.AuthData.Length);

						var auth_message = serializer.MessageWriter.ToMessage(true);
						auth_message[0].FrameType = MqFrameType.Command;

						// RpcCommand:byte; RpcCommandType:byte; AuthData:byte[];
						Send(auth_message);
					} else {
						// If no authentication is required, set this client to authenticated.
						Authenticated = true;
					}

					// Alert the server that this session is ready for usage.
					WorkerThreadPool.QueueWorkItem(() => {
						Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this));
					});

					SerializationCache.Put(serializer);

				} else if (rpc_command_type == RpcCommandType.AuthenticationRequest) {
					// RpcCommand:byte; RpcCommandType:byte; AuthData:byte[];

					// If this is not run on the server, quit.
					if (BaseSocket.Mode != SocketMode.Server) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					// Ensure that the server requires authentication.
					if (Server.Config.RequireAuthentication == false) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					byte[] auth_bytes = new byte[frame.DataLength - 2];
					frame.Read(2, auth_bytes, 0, auth_bytes.Length);

					var auth_args = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this) {
						AuthData = auth_bytes
					};


					Authenticate?.Invoke(this, auth_args);

					Authenticated = auth_args.Authenticated;

					var auth_frame = CreateFrame(new byte[auth_args.AuthData.Length + 2], MqFrameType.Command);
					auth_frame.Write(0, (byte) MqCommandType.RpcCommand);
					auth_frame.Write(1, (byte) RpcCommandType.AuthenticationResult);

					// State of the authentication
					auth_frame.Write(2, Authenticated);

					// RpcCommand:byte; RpcCommandType:byte; AuthResult:bool;
					Send(auth_frame);

					if (Authenticated == false) {
						Close(SocketCloseReason.AuthenticationFailure);
					} else {
						// Alert the server that this session is ready for usage.
						WorkerThreadPool.QueueWorkItem(() => {
							Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this));
						});
					}

				} else if (rpc_command_type == RpcCommandType.AuthenticationResult) {
					// RpcCommand:byte; RpcCommandType:byte; AuthResult:bool;

					// Ensure that this command is running on the client.
					if (BaseSocket.Mode != SocketMode.Client) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					if (Client.Config.RequireAuthentication == false) {
						Close(SocketCloseReason.ProtocolError);
						return;
					}

					Authenticated = true;

					var auth_args = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this) {
						Authenticated = frame.ReadBoolean(2)
					};

					// Alert the client that the sesion has been authenticated.
					AuthenticationResult?.Invoke(this, auth_args);

					// Alert the client that this session is ready for usage.
					WorkerThreadPool.QueueWorkItem(() => {
						Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this));
					});

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

				serializer.MessageWriter.Write((byte) MqCommandType.RpcCommand);
				serializer.MessageWriter.Write((byte) RpcCommandType.WelcomeMessage);
				serializer.SerializeToWriter(Server.ServerInfo);

				var message = serializer.MessageWriter.ToMessage(true);

				message[0].FrameType = MqFrameType.Command;

				// RpcCommand:byte; RpcCommandType:byte; RpcServerInfoDataContract:byte[];
				Send(message);
			}

			base.OnConnected();

			// If the server does not require authentication, alert the server session that it is ready.
			if (BaseSocket.Mode == SocketMode.Server && Config.RequireAuthentication == false) {
				Authenticated = true;
				Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession)this));
			}
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

				// Read the first byte for the ID.
				var handler_id = message[0].ReadByte(0);
				var handled_message = false;

				// See if we have a handler for the requested Id.
				if (MessageHandlers.ContainsKey(handler_id)) {
					handled_message = MessageHandlers[handler_id].HandleMessage(message);
				}

				// If the we can not handle this message, disconnect the session.
				if (handled_message == false) {
					Close(SocketCloseReason.ProtocolError);
					return;
				}
			}
		}


		/// <summary>
		/// Adds a proxy interface and instance to the current session to allow for remote method proxying.
		/// </summary>
		/// <typeparam name="T">Interface of the instance.  Must be explicitly specified.</typeparam>
		/// <param name="instance">Instance of the interface implementation.</param>
		public void AddProxy<T>(T instance) where T : IRemoteService<TSession, TConfig> {
			var proxy = new RpcProxy<T, TSession, TConfig>(instance, this, RpcCallHandler);

			RpcCallHandler.RemoteServiceRealproxy.Add(typeof(T), proxy);
			RpcCallHandler.RemoteServicesProxy.Add(typeof(T), (T) proxy.GetTransparentProxy());
		}

		/// <summary>
		/// Returns the proxy of the specified type if it exists on this session.
		/// </summary>
		/// <typeparam name="T">Interface of the proxy to retrieve.</typeparam>
		/// <returns>Proxied interface methods.</returns>
		public T GetProxy<T>() where T : IRemoteService<TSession, TConfig> {
			return (T)RpcCallHandler.RemoteServicesProxy[typeof(T)];
		}

		/// <summary>
		/// Adds a service to this session to be called remotely.
		/// </summary>
		/// <typeparam name="T">Interface of this type.</typeparam>
		/// <param name="instance">Instance to execute methods on.</param>
		public void AddService<T>(T instance) where T : IRemoteService<TSession, TConfig> {
			RpcCallHandler.Services.Add(instance.Name, instance);
			instance.Session = (TSession) this;
		}



		



	}
}
