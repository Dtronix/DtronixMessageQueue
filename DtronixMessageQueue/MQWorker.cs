using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQWorker : IDisposable {
		private readonly MQSession connector;
		private readonly Task worker_task;
		private long average_idle_time = 2000;

		/// <summary>
		/// Average time this worker remains idle.
		/// The smaller the number, the more work being done.
		/// </summary>
		public long AverageIdleTime => average_idle_time;

		private readonly CancellationTokenSource cancellation_source = new CancellationTokenSource();

		private readonly Action<object> work;

		public MQWorker(Action<object> work, MQSession connector) {
			this.work = work;
			this.connector = connector;
			worker_task = new Task(this.work, cancellation_source.Token, cancellation_source.Token, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Start the worker.
		/// </summary>
		public void Start() {
			worker_task.Start();
		}

		/// <summary>
		/// Interrupt the worker loop and keep the worker in an idle state.
		/// </summary>
		public void Stop() {
			cancellation_source.Cancel();
		}

		private void ProcessQueue(object o) {
			var token = (CancellationToken) o;
			var idle_stopwatch = new Stopwatch();

			while (token.IsCancellationRequested == false) {
				idle_stopwatch.Restart();
				
				// Check the average time this thread remains idle
				average_idle_time = average_idle_time == -1
					? idle_stopwatch.ElapsedMilliseconds
					: (idle_stopwatch.ElapsedMilliseconds + average_idle_time) / 2;

				try {
					work?.Invoke(token);
				} catch (Exception) {
					// ignored
				}
			}
		}

		public void Dispose() {
			if (worker_task.IsCanceled == false) {
				Stop();
			}
		}
	}
}
