using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace DtronixMessageQueue {
	class MqReceiveFilterFactory : IReceiveFilterFactory<BinaryRequestInfo> {
		public IReceiveFilter<BinaryRequestInfo> CreateFilter(IAppServer app_server, IAppSession app_session, IPEndPoint remote_end_point) {
			return new MqReceiveFilter();
		}
	}
}
