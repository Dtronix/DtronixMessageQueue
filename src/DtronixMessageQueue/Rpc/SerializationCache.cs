using System.Collections.Concurrent;
using System.IO;

namespace DtronixMessageQueue.Rpc {

	/// <summary>
	/// Used to cache readers, writers and memory streams used for deserializing.
	/// </summary>
	public class SerializationCache {

		/// <summary>
		/// Configurations for this connection to use.
		/// </summary>
		private readonly RpcConfig config;

		/// <summary>
		/// Class which contains the cached class instances.
		/// </summary>
		public class Container {
			/// <summary>
			/// Writer
			/// </summary>
			public MqMessageWriter MessageWriter;

			/// <summary>
			/// Reader
			/// </summary>
			public MqMessageReader MessageReader;

			/// <summary>
			/// Memory stream used for object serializing.
			/// </summary>
			public MemoryStream Stream;
		}

		/// <summary>
		/// Contains all available cached containers.
		/// </summary>
		private readonly ConcurrentQueue<Container> cached_containers = new ConcurrentQueue<Container>();

		/// <summary>
		/// Creates an instance of the serializing cache with the specified configurations.
		/// </summary>
		/// <param name="config">Configurations for this session.</param>
		public SerializationCache(RpcConfig config) {
			this.config = config;
		}

		/// <summary>
		/// Gets an available cached instance of the serializer.  If one is not cached, generate a new one.
		/// </summary>
		/// <returns></returns>
		public Container Get() {
			Container container;

			// Try to get an existing cached container.
			if (cached_containers.TryDequeue(out container) == false) {
				// A cached container does not exist.  Create a new one.
				var mq_writer = new MqMessageWriter(config);
				var mq_reader = new MqMessageReader();

				container = new Container {
					MessageWriter = mq_writer,
					MessageReader = mq_reader,
					Stream = new MemoryStream()

				};
			} else {
				// Reset the existing cached container to the defaults to allow for pristine usage.
				container.MessageWriter.Clear();
				container.Stream.SetLength(0);
			}
			
			return container;
		}

		/// <summary>
		/// Returns a used container for future usage.
		/// </summary>
		/// <param name="container">Container to return.</param>
		public void Put(Container container) {
			cached_containers.Enqueue(container);
		}


	}
}
