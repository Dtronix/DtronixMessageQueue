using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {

	/// <summary>
	/// Specifies the type of command that is being requested/executed.
	/// </summary>
	public enum MqCommandType : byte {

		/// <summary>
		/// Command instructs this session to disconnect.
		/// </summary>
		Disconnect = 0,

		/// <summary>
		/// Command is part of the base message queue process.
		/// </summary>
		MqCommand = 1,

		/// <summary>
		/// Command is part of the Rpc process.
		/// </summary>
		RpcCommand = 2
	}
}
