using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class IncomingMessageEventArgs : EventArgs {

		public MQMailbox Mailbox { get; set; }

		public Guid Id { get; set; }

		public IncomingMessageEventArgs(MQMailbox mailbox, Guid id) {
			Id = id;
			Mailbox = mailbox;
		}
	}
}
