using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application
{
    public class ApplicationConfig
    {
        public MqLogger Logger { get; set; }

        public TransportConfig TransportConfig { get; set; }
    }
}