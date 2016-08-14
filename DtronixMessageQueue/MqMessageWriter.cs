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
		private int message_index = 0;
		private int frame_position = 0;
		private byte[] frame_buffer;

		public MqMessage Message {
			get { return message; }
			set { message = value; }
		}


		public MqMessageWriter(int buffer_size) : this(buffer_size, new MqMessage()) {
			
		}

		public MqMessageWriter(int buffer_size, MqMessage message) {
			this.message = message;
			frame_buffer = new byte[buffer_size];

		}

		private byte[] GetCurrentBuffer() {
			return message_index + 1 > message.Count ? frame_buffer : message[message_index].Buffer;
		}




	}
}
