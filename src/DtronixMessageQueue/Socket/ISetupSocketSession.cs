﻿using Amib.Threading;

namespace DtronixMessageQueue.Socket {

	/// <summary>
	/// Class to implement on classes which have setup events.
	/// </summary>
	/// <typeparam name="TConfig"></typeparam>
	public interface ISetupSocketSession<TConfig>
		where TConfig : SocketConfig {

		/// <summary>
		/// Sets up this socket with the specified configurations.
		/// </summary>
		/// <param name="session_socket">Socket this session is to use.</param>
		/// <param name="socket_args_pool">Argument pool for this session to use.  Pulls two asyncevents for reading and writing and returns them at the end of this socket's life.</param>
		/// <param name="session_config">Socket configurations this session is to use.</param>
		/// <param name="thread_pool">Thread pool used by the socket to read and write.</param>
		void Setup(System.Net.Sockets.Socket session_socket, SocketAsyncEventArgsPool socket_args_pool, TConfig session_config,
			SmartThreadPool thread_pool);

		/// <summary>
		/// Start the session's receive events.
		/// </summary>
		void Start();
	}
}