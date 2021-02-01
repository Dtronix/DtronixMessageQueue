using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue
{
    public class MqConfig : SocketConfig
    {
        /// <summary>
        /// Max size of the frame.  Needs to be equal or smaller than SendAndReceiveBufferSize.
        /// Need to exclude the header length for a frame along with the TCP socket encryption header.
        /// 
        /// </summary>
        public int FrameBufferSize { get; set; } = 1024 * 16 - MqFrame.HeaderLength - 3;

        /// <summary>
        /// (Client) 
        /// Milliseconds between pings.
        /// 0 disables pings.
        /// </summary>
        public int PingFrequency { get; set; } = 0;

        /// <summary>
        /// Sets a limit on the maximum outgoing queue size.
        /// Once the outgoing queue reaches the maximum messages, the MqSession.Send will block.
        /// </summary>
        public int MaxQueuedOutgoingMessages { get; set; } = 50;

        /// <summary>
        /// Sets a limit on the maximum inbound byte queue size.
        /// Once the incoming queue reaches the maximum messages, the incoming parsing queue will block.
        /// </summary>
        public int MaxQueuedInboundPackets { get; set; } = 20;
    }
}