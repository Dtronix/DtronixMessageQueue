using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	public class MqServer {
		//private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public class Config {

			public int MaxConnections { get; set; } = 1;

			/// <summary>
			/// Maximum backlog for pending connections.
			/// The default value is 100.
			/// </summary>
			public int ListenerBacklog { get; set; } = 100;
		}

		private readonly Config configurations;

		private readonly MqSuperServer server;


		public MqServer(Config configurations) {
			this.configurations = configurations;
			server = new MqSuperServer();


			server.NewRequestReceived += ServerOnNewRequestReceived;
			server.NewSessionConnected += ServerOnNewSessionConnected;
			server.SessionClosed += ServerOnSessionClosed;
		}

		private void ServerOnSessionClosed(MqSession session, CloseReason value) {
			

		}

		private void ServerOnNewSessionConnected(MqSession session) {
			

		}

		private void ServerOnNewRequestReceived(MqSession session, RequestInfo<byte, byte[]> request_info) {
			

			connection.Mailbox.EnqueueIncomingBuffer(buffer);

		}

		public void CloseConnection(MqSession session, CloseReason reason) {
			session.Close(reason);
		}
	}

}
