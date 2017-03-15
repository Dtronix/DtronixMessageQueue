using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace DtronixMessageQueue.Rpc.DataContract {

	/// <summary>
	/// Used to transport the description of the server to the client.
	/// </summary>
	[ProtoContract]
	public class RpcServerInfoDataContract {

		/// <summary>
		/// Version of this server.
		/// </summary>
		[ProtoMember(1)]
		public string Version { get; set; } = new Version(1, 1).ToString();

		/// <summary>
		/// Arbitrary data that is sent along in the MqFrame
		/// </summary>
		[ProtoMember(2)]
		public byte[] Data { get; set; }

		/// <summary>
		/// True if the server requires authentication before proceeding; False otherwise.
		/// </summary>
		[ProtoMember(3)]
		public bool RequireAuthentication { get; set; }

		/// <summary>
		/// Session ID used for 
		/// </summary>
		[ProtoMember(4)]
		public Guid SessionId { get; set; }

		/// <summary>
		/// AES key used for all data
		/// </summary>
		[ProtoMember(5)]
		public byte[] SessionEncryptionKey { get; set; }

		/// <summary>
		/// AES IV used for all data
		/// </summary>
		[ProtoMember(6)]
		public byte[] SessionEncryptionIv { get; set; }


	}
}
