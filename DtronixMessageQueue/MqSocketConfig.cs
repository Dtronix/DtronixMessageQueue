using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue {
	public class MqSocketConfig : SocketConfig {

		/// <summary>
		/// Max size of the frame.  Needs to be equal or smaller than SendAndReceiveBufferSize.
		/// Need to exclude the header length for a frame.
		/// </summary>
		public int FrameBufferSize { get; set; } = 1024 * 4 - MqFrame.HeaderLength;

		/// <summary>
		/// (Client) 
		/// Milliseconds between pings.
		/// </summary>
		public int PingFrequency { get; set; } = -1;

		/// <summary>
		/// (Server)
		/// Max milliseconds since the last received packet before the session is disconnected.
		/// </summary>
		public int PingTimeout { get; set; } = 60000;
	}
}
