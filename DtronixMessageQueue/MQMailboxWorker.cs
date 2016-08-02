using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQMailboxWorker : IDisposable {
		private Thread worker_thread;

		private long average_idle_time = -1;
		private bool is_running = false;

		public MQMailboxWorker() : this("mq-default-mailbox-worker") {

		}

		public MQMailboxWorker(string thread_name) {
			worker_thread = new Thread(ProcessQueue) {
				IsBackground = true,
				Name = thread_name
			};
		}

		/// <summary>
		/// Start the worker.
		/// </summary>
		public void Start() {
			is_running = true;
			worker_thread.Start();
		}

		/// <summary>
		/// Interrupt the worker loop and keep the worker in an idle state.
		/// </summary>
		public void Stop() {
			is_running = false;
		}

		private void ProcessQueue() {
			var idle_stopwatch = new Stopwatch();

			while (is_running) {
				idle_stopwatch.Restart();
				
				// Check the average time this thread remains idle
				average_idle_time = average_idle_time == -1
					? idle_stopwatch.ElapsedMilliseconds
					: (idle_stopwatch.ElapsedMilliseconds + average_idle_time) / 2;


			
			}

			idle_stopwatch.Stop();
		}

		public void Dispose() {
			if (worker_thread.IsAlive) {
				Stop();
				worker_thread.Abort();
			}
		}
	}
}
