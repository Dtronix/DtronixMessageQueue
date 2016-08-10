using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MqMessageReader {
		private int position;
		private MqMessage message;

		public int Position {
			get { return position; }
			set { position = value; }
		}

		public MqMessage Message {
			get { return message; }
			set { message = value; }
		}

		public MqMessageReader(MqMessage message) {
			this.message = message;
		}
	}
}
