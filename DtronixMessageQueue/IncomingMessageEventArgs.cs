using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class IncomingMessageEventArgs : EventArgs {

		public MQMailbox Mailbox { get; set; }

		public MQSession Connection { get; set; }

		public IncomingMessageEventArgs(MQSession connection) {
			Connection = connection;
			Mailbox = connection.Mailbox;
		}
	}
}
