using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQMailbox : ConcurrentQueue<MQMessage> {
		public MQMailbox() {
			
		}
	}
}
