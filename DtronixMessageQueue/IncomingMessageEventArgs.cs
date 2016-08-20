using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class IncomingMessageEventArgs : EventArgs {
		public MqMailbox Mailbox { get; set; }
		public MqSession Session { get; set; }

		public IncomingMessageEventArgs(MqMailbox mailbox, MqSession session) {
			Mailbox = mailbox;
			Session = session;
		}
	}
}