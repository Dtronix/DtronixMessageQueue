﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Performance.Services.Server {
	class TestService : MarshalByRefObject, ITestService {
		public string Name { get; } = "TestService";
		public SimpleRpcSession Session { get; set; }

		public event EventHandler<SimpleRpcSession> Completed;

		private int call_count = 0;
		private int total_calls = 0;

		private bool completed = false;

		public void TestNoReturn() {
			call_count++;
			VerifyComplete();

		}

		public int TestIncrement() {
			call_count++;
			VerifyComplete();
			return call_count;
		}

		public void TestSetup(int calls) {
			total_calls = calls;
		}

		private void VerifyComplete() {
			if (completed == false && total_calls == call_count) {
				completed = true;
				Completed?.Invoke(this, Session);
			}
		}


	}

	internal interface ITestService : IRemoteService<SimpleRpcSession> {
		void TestNoReturn();
		int TestIncrement();
		void TestSetup(int calls);
	}
}
