using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
			var number = Interlocked.Increment(ref call_count);
			//Console.Write($"{Thread.CurrentThread.ManagedThreadId} ");

			VerifyComplete();

		}

		public async void TestNoReturnAwait() {
			var number = Interlocked.Increment(ref call_count);
			await Task.Delay(1000);

			VerifyComplete();

		}

		public void TestNoReturnLongBlocking() {
			var number = Interlocked.Increment(ref call_count);
			Thread.Sleep(10000);
			VerifyComplete();

		}


		public void TestNoReturnBlock() {
			Task.Factory.StartNew(() => {
				var number = Interlocked.Increment(ref call_count);

				Thread.Sleep(1000);
				VerifyComplete();
			}, TaskCreationOptions.LongRunning);
		}

		public int TestIncrement() {
			call_count++;
			VerifyComplete();
			return call_count;
		}

		public void TestSetup(int calls) {
			total_calls = calls;
		}

		public bool ResetTest() {
			call_count = 0;
			completed = false;
			return true;
		}

		public int TestException() {
			call_count++;
			VerifyComplete();
			throw new Exception("This is a test exception");
		}

		private void VerifyComplete() {
			if (completed == false && total_calls == call_count) {
				completed = true;
				Completed?.Invoke(this, Session);
			}
		}


	}

	internal interface ITestService : IRemoteService<SimpleRpcSession, RpcConfig> {
		void TestNoReturn();
		void TestNoReturnAwait();
		void TestNoReturnBlock();
		void TestNoReturnLongBlocking();
		int TestIncrement();
		void TestSetup(int calls);
		bool ResetTest();
		int TestException();
	}
}
