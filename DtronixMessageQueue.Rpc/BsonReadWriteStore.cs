using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace DtronixMessageQueue.Rpc {
	public class BsonReadWriteStore {
		private readonly MqSocketConfig config;

		public class Store {
			public MqMessageWriter MessageWriter;
			public BsonWriter BsonWriter;
			public JsonSerializer Serializer;
			public MqMessageReader MessageReader;
			public BsonReader BsonReader;
		}

		private ConcurrentQueue<Store> reader_writers = new ConcurrentQueue<Store>();

		public BsonReadWriteStore(MqSocketConfig config) {
			this.config = config;
		}

		public Store Get() {
			Store store;
			if (reader_writers.TryDequeue(out store) == false) {

				var mq_writer = new MqMessageWriter(config);
				var bson_writer = new BsonWriter(mq_writer);
				var mq_reader = new MqMessageReader();
				var bson_reader = new BsonReader(mq_reader) {
					CloseInput = false,
					SupportMultipleContent = true
				};
				var json_serializer = new JsonSerializer {
					NullValueHandling = NullValueHandling.Ignore,
					CheckAdditionalContent = false,


				};

				store = new Store {
					MessageWriter = mq_writer,
					BsonWriter = bson_writer,
					Serializer = json_serializer,
					MessageReader = mq_reader,
					BsonReader = bson_reader

				};
			} else {
				store.MessageWriter.Clear();

				// Hack to reset BsonRead
				store.BsonReader = new BsonReader(store.MessageReader);
			}
			
			//
			
			
			return store;
		}

		public void Put(Store store) {

			reader_writers.Enqueue(store);
		}


	}
}
