using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public enum MqFrameType : byte {
		/// <summary>
		/// This frame type has not been defined.
		/// </summary>
		Unset,

		/// <summary>
		/// This frame type has not been determined yet.
		/// </summary>
		Empty,

		/// <summary>
		/// This frame is part of a larger message.
		/// </summary>
		More,

		/// <summary>
		/// This frame is the last part of a message.
		/// </summary>
		Last,

		/// <summary>
		/// This frame is an empty frame and the last part of a message.
		/// </summary>
		EmptyLast
	}
}