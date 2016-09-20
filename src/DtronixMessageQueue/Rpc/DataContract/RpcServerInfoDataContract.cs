using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace DtronixMessageQueue.Rpc.DataContract {

	/// <summary>
	/// Used to transport teh description of the server to the client.
	/// </summary>
	[ProtoContract]
	public class RpcServerInfoDataContract {

		/// <summary>
		/// Version of this server.
		/// </summary>
		[ProtoMember(1)]
		public Version Version { get; set; } = new Version(1, 0);

		/// <summary>
		/// Message that the server send to the client.
		/// </summary>
		[ProtoMember(2)]
		public string Message { get; set; } = "RpcServer";

		/// <summary>
		/// Arbitrary data that is sent along in the MqFrame
		/// </summary>
		[ProtoMember(3)]
		public byte[] Data { get; set; }
	}
}
