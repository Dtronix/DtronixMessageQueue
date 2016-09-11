using System.Collections.Concurrent;
using System.IO;

namespace DtronixMessageQueue.Rpc {
	public class BsonReadWriteStore {
		private readonly MqSocketConfig config;

		public class Store {
			public MqMessageWriter MessageWriter;
			public MqMessageReader MessageReader;
			public MemoryStream Stream;
		}

		private readonly ConcurrentQueue<Store> reader_writers = new ConcurrentQueue<Store>();

		public BsonReadWriteStore(MqSocketConfig config) {
			this.config = config;
		}

		public Store Get() {
			Store store;
			if (reader_writers.TryDequeue(out store) == false) {

				var mq_writer = new MqMessageWriter(config);
				var mq_reader = new MqMessageReader();

				store = new Store {
					MessageWriter = mq_writer,
					MessageReader = mq_reader,
					Stream = new MemoryStream()

				};
			} else {
				store.MessageWriter.Clear();
				store.Stream.SetLength(0);
			}
			
			//
			
			
			return store;
		}

		public void Put(Store store) {
			reader_writers.Enqueue(store);
		}


	}
}
