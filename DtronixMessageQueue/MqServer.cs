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
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	public class MqServer : AppServer<MqSession, RequestInfo<byte, byte[]>> {
		public MqPostmaster Postmaster { get; }

		public MqServer(ServerConfig server_config) : this(null, server_config) {
		}

		public event EventHandler<IncomingMessageEventArgs> IncomingMessage;

		public event EventHandler Started;

		public MqServer(RootConfig root_config, ServerConfig server_config) : base(new MqServerReceiveFilterFactory()) {
			if (root_config == null) {
				root_config = new RootConfig {
					MaxCompletionPortThreads = 100,
					MaxWorkingThreads = 100,
					MinCompletionPortThreads = 5,
					MinWorkingThreads = 5,
					Isolation = IsolationMode.None,
					PerformanceDataCollectInterval = 0,
					DisablePerformanceDataCollector = true
				};
			}

			if (server_config == null) {
				throw new ArgumentNullException(nameof(server_config));
			}

			server_config.MaxRequestLength = MqFrame.MaxFrameSize + 3;
			server_config.ReceiveBufferSize = (MqFrame.MaxFrameSize + 3) * 2;

			Postmaster = new MqPostmaster();

			Setup(root_config, server_config, null);
		}

		protected override MqSession CreateAppSession(ISocketSession socket_session) {
			var session = new MqSession();
			session.Mailbox = new MqMailbox(Postmaster, session);

			// TODO: Review how to do this better.
			session.Mailbox.IncomingMessage += OnIncomingMessage;

			return session;
		}

		protected override void OnStarted() {
			Started?.Invoke(this, new EventArgs());
			base.OnStarted();
		}

		private void OnIncomingMessage(object sender, IncomingMessageEventArgs e) {
			IncomingMessage?.Invoke(sender, e);
		}

		protected override void OnSessionClosed(MqSession session, CloseReason reason) {
			session.Mailbox.IncomingMessage -= OnIncomingMessage;
			session.Mailbox.Dispose();
			base.OnSessionClosed(session, reason);
		}

		protected override void ExecuteCommand(MqSession session, RequestInfo<byte, byte[]> request_info) {
			try {
				if (request_info.Header == 0) {
					session.Mailbox.EnqueueIncomingBuffer(request_info.Body);
					//} else if (request_info.Header == 1) {
					// TODO: Setup configurations that the client must send before successfully connecting.
					// request_info.
				} else {
					// If the first byte is anything else, the data is either corrupted or another protocol.
					session.Close(CloseReason.ApplicationError);
				}
			} catch (Exception) {
				session.Close(CloseReason.ApplicationError);
				return;
			}
			base.ExecuteCommand(session, request_info);
		}
	}
}