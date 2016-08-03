using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class IncomingMessageEventArgs : EventArgs {

		public MQMailbox Mailbox { get; set; }

		public MQConnection Connection { get; set; }

		public IncomingMessageEventArgs(MQConnection connection) {
			Connection = connection;
			Mailbox = connection.Mailbox;
		}
	}
}
