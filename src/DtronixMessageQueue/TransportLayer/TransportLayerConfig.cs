using DtronixMessageQueue.TransportLayer.Tcp;

namespace DtronixMessageQueue.TransportLayer
{
    /// <summary>
    /// Configurations for the server/client.
    /// </summary>
    public class TransportLayerConfig
    {
        /// <summary>
        /// Maximum number of connections allowed.  Only used by the server.
        /// </summary>
        public int MaxConnections { get; set; } = 1000;

        /// <summary>
        /// Maximum backlog for pending connections.
        /// The default value is 100.
        /// </summary>
        public int ListenerBacklog { get; set; } = 100;

        /// <summary>
        /// Size of the buffer for the sockets.
        /// </summary>
        public int SendAndReceiveBufferSize { get; set; } = 1024 * 16;

        /// <summary>
        /// Time in milliseconds it takes for the sending event to fail.
        /// </summary>
        public int SendTimeout { get; set; } = 5000;

        /// <summary>
        /// (Client) Time in milliseconds it takes to timeout a connection attempt.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 60000;

        /// <summary>
        /// (Client) Connection address.
        /// Parses standard IP:Port notation.
        /// </summary>
        public string ConnectAddress { get; set; }

        /// <summary>
        /// (Server) Connection binding address
        /// Parses standard IP:Port notation.
        /// </summary>
        public string BindAddress { get; set; }

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
        /// The default transport layer to use.  If left null, Tcp will the be default layer.
        /// </summary>
        public ITransportLayer TransportLayer { get; set; }


    }
}