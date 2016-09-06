using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Performance.Services.Server {
	class TestService : MarshalByRefObject, ITestService {
		public string Name { get; } = "TestService";
		public SimpleRpcSession Session { get; set; }

		private int call_count = 0;
		private int total_calls = 0;


		public void TestNoReturn() {
			call_count++;

		}

		public int TestIncrement() {
			throw new NotImplementedException();
		}

		public void TestSetup(int calls) {
			total_calls = calls;
		}


	}

	internal interface ITestService : IRemoteService<SimpleRpcSession> {
		void TestNoReturn();
		int TestIncrement();
		void TestSetup(int calls);
	}
}
