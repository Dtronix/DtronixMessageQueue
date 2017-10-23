using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue
{
    public class MqConfig : TransportLayerConfig
    {
        /// <summary>
        /// Max size of the frame.  Needs to be equal or smaller than SendAndReceiveBufferSize.
        /// Need to exclude the header length for a frame.
        /// </summary>
        public int FrameBufferSize { get; set; } = 1024 * 16 - MqFrame.HeaderLength;

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

        /// <summary>
        /// (Server)
        /// Number of threads used to read and write.
        /// If set to -1 (default), it will use the number of logical processors.
        /// </summary>
        public int ProcessorThreads { get; set; } = -1;

        /// <summary>
        /// (Server/Client)
        /// Max milliseconds since the last received packet before the session is disconnected.
        /// 0 disables the automatic disconnection functionality.
        /// </summary>
        public int PingTimeout { get; set; } = 60000;

        /// <summary>
        /// (Client) 
        /// Milliseconds between pings.
        /// 0 disables pings.
        /// </summary>
        public int PingFrequency { get; set; } = 3000;

        /// <summary>
        /// (Client) Time in milliseconds it takes to timeout a connection attempt.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 60000;
    }
}