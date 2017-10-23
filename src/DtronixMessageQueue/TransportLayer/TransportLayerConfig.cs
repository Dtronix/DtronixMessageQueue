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
        /// The default transport layer to use.  If left null, Tcp will the be default layer.
        /// </summary>
        public ITransportLayer TransportLayer { get; set; }


    }
}