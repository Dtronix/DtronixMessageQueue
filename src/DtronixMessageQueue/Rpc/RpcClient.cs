﻿using System;
using System.Configuration;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Rpc class for containing client logic.
	/// </summary>
	/// <typeparam name="TSession">Session type for this connection.</typeparam>
	/// <typeparam name="TConfig">Configuration for this connection.</typeparam>
	public class RpcClient<TSession, TConfig> : MqClient<TSession, TConfig>
		where TSession : RpcSession<TSession, TConfig>, new()
		where TConfig : RpcConfig {

		/// <summary>
		/// Information about the connected server.
		/// </summary>
		public RpcServerInfoDataContract ServerInfo { get; set; }

		/// <summary>
		/// Called to send authentication data to the server.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs<TSession,TConfig>> Authenticate;

		/// <summary>
		/// Called when the authentication process succeeds.
		/// </summary>
		public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> AuthenticationSuccess;

		/// <summary>
		/// Event invoked once when the RpcSession has been authenticated and is ready for usage.
		/// </summary>
		public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;

		/// <summary>
		/// Initializes a new instance of a Rpc client.
		/// </summary>
		/// <param name="config">Configurations for this client to use.</param>
		public RpcClient(TConfig config) : base(config) {

			// Check on thew encryption configurations.
			if (config.EncryptionKey != null) {
				var key_length = config.EncryptionKey.Length;

				// Verify the key length
				if (key_length != 128 && key_length != 196 && key_length != 256) {
					throw new ConfigurationErrorsException("Encryption configuration does not specify a key of the proper length");
				}
			}
		}

		protected override TSession CreateSession(System.Net.Sockets.Socket session_socket) {
			var session = base.CreateSession(session_socket);

			session.Ready += (sender, e) => { Ready?.Invoke(sender, e); };
			session.Authenticate += (sender, e) => { Authenticate?.Invoke(sender, e); };
			session.AuthenticationSuccess += (sender, e) => { AuthenticationSuccess?.Invoke(sender, e); };
			return session;
		}
	}
}
