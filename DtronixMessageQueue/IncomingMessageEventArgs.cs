using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {

	/// <summary>
	/// Event args for when a new message has been processed and is ready for usage.
	/// </summary>
	public class IncomingMessageEventArgs : EventArgs {

		/// <summary>
		/// Reference to the mailbox with the new message.
		/// </summary>
		public MqMailbox Mailbox { get; set; }

		/// <summary>
		/// If this message is on the server, this will contain the reference to the connected session of the client.
		/// </summary>
		public MqSession Session { get; set; }

		/// <summary>
		/// If this message on the client, this will contain the reference to the client.
		/// </summary>
		public MqClient Client { get; set; }

		/// <summary>
		/// Creates an instance of the event args.
		/// </summary>
		/// <param name="mailbox">Mailbox with the new message</param>
		/// <param name="session">Server session.  Null if this is on the client.</param>
		/// <param name="client">Client.  Null if this is on the server.</param>
		public IncomingMessageEventArgs(MqMailbox mailbox, MqSession session, MqClient client) {
			Mailbox = mailbox;
			Session = session;
			Client = client;
		}
	}
}