using System;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {
	public class MqConfig : SocketConfig {
		private int max_read_write_workers = 4;

		/// <summary>
		/// Max size of the frame.  Needs to be equal or smaller than SendAndReceiveBufferSize.
		/// Need to exclude the header length for a frame.
		/// </summary>
		public int FrameBufferSize { get; set; } = 1024 * 16 - MqFrame.HeaderLength;

		/// <summary>
		/// (Client) 
		/// Milliseconds between pings.
		/// 0 disables pings.
		/// </summary>
		public int PingFrequency { get; set; } = 0;

		/// <summary>
		/// (Server)
		/// Max milliseconds since the last received packet before the session is disconnected.
		/// 0 disables the automatic disconnection functionality.
		/// </summary>
		public int PingTimeout { get; set; } = 60000;


		/// <summary>
		/// (Server/Client)
		/// Max number of workers used to read/write.
		/// Minimum of 4 required.
		/// </summary>
		/// <remarks>
		/// Writers and readers share the total thread count specified here.
		/// </remarks>
		public int MaxReadWriteWorkers {
			get { return max_read_write_workers; }
			set {
				max_read_write_workers = Math.Max(4, value);
			}
		}


		/// <summary>
		/// (Server/Client)
		/// Time in milliseconds that it takes for an idle worker to close down.
		/// </summary>
		public int IdleWorkerTimeout { get; set; } = 60000;
	}
}
