using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	internal class MqServerReceiveFilterFactory : IReceiveFilterFactory<RequestInfo<byte, byte[]>> {
		public IReceiveFilter<RequestInfo<byte, byte[]>> CreateFilter(IAppServer app_server, IAppSession app_session, IPEndPoint remote_end_point) {
			return new MqServerReceiveFilter();
		}
	}
}
