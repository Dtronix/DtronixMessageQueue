using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {

	/// <summary>
	/// Writer to aid in the creation of messages and their frames.
	/// Based upon BinaryWriter code
	/// </summary>
	/// <remarks>
	/// https://github.com/dotnet/corefx/blob/master/src/System.IO/src/System/IO/BinaryWriter.cs
	/// </remarks>
	public class MqMessageWriter {
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

		public MqMessageWriter(MqMessage message) {
			this.message = message;
		}

		public MqMessageWriter() {
			message = new MqMessage();

			//new BinaryWriter()
		}


	}
}
