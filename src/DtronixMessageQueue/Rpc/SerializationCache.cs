using System;
using System.Collections.Concurrent;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

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
		public class Serializer {
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

			/// <summary>
			/// Reads the rest of the message reader's bytes to prepare for deserialization.
			/// </summary>
			public void PrepareDeserializeReader() {
				// Reads the rest of the message for the return value.
				var return_bytes = MessageReader.ReadToEnd();

				// Set the stream to 0 and write the content of the return value for deserialization
				Stream.SetLength(0);
				Stream.Write(return_bytes, 0, return_bytes.Length);
				Stream.Position = 0;
			}


			/// <summary>
			/// Deserialize with the specified type.  If a field number is specified, a length is prefixed to the data.
			/// </summary>
			/// <param name="type">Type to attempt to deserialize.</param>
			/// <param name="field_number">Identification number for the protobuf to deserialize with.</param>
			/// <returns></returns>
			public object DeserializeFromReader(Type type, int field_number = -1) {
				if (field_number != -1) {
					return RuntimeTypeModel.Default.DeserializeWithLengthPrefix(Stream, null, type, PrefixStyle.Base128, field_number);
				}

				return RuntimeTypeModel.Default.Deserialize(Stream, null, type);
			}



			/// <summary>
			/// Serializes the data to the the message writer.  If a field number is specified, a length is prefixed to the data and will be read first.
			/// </summary>
			/// <param name="value">Value to serialize.</param>
			/// <param name="field_number">Identification number for the protobuf to serialize with.</param>
			public void SerializeToWriter(object value, int field_number = -1) {
				// Reset the stream
				Stream.SetLength(0);

				// Serialize with a length prefix to allow for simplification of deserialization.
				if (field_number != -1) {
					RuntimeTypeModel.Default.SerializeWithLengthPrefix(Stream, value, value.GetType(), PrefixStyle.Base128, field_number);
				} else {
					RuntimeTypeModel.Default.Serialize(Stream, value.GetType());
				}

				// Write the stream data to the message.
				MessageWriter.Write(Stream.ToArray());
			}
		}

		/// <summary>
		/// Contains all available cached containers.
		/// </summary>
		private readonly ConcurrentQueue<Serializer> cached_containers = new ConcurrentQueue<Serializer>();

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
		public Serializer Get() {
			Serializer serializer;

			// Try to get an existing cached serializer.
			if (cached_containers.TryDequeue(out serializer) == false) {
				// A cached serializer does not exist.  Create a new one.
				var mq_writer = new MqMessageWriter(config);
				var mq_reader = new MqMessageReader();

				serializer = new Serializer {
					MessageWriter = mq_writer,
					MessageReader = mq_reader,
					Stream = new MemoryStream()
				};
			} else {
				serializer.Stream.SetLength(0);
				serializer.MessageWriter.Clear();
			}
			
			return serializer;
		}

		/// <summary>
		/// Returns a used serializer for future usage.
		/// </summary>
		/// <param name="serializer">Serializer to return.</param>
		public void Put(Serializer serializer) {
			serializer.Stream.SetLength(0);
			serializer.MessageWriter.Clear();
			cached_containers.Enqueue(serializer);
		}


	}
}
