using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class IncomingMessageEventArgs : EventArgs {

		public MqMailbox Mailbox { get; set; }

		public MqSession Connection { get; set; }

		public IncomingMessageEventArgs(MqSession connection) {
			Connection = connection;
			Mailbox = connection.Mailbox;
		}
	}
}
