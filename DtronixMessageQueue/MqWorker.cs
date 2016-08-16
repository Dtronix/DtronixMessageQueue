using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	internal class MqWorker : IDisposable {
		private readonly Task worker_task;
		private long average_idle_time = 0;
		private long average_work_time = 0;
		private readonly Stopwatch idle_stopwatch = new Stopwatch();
		private readonly Stopwatch work_stopwatch = new Stopwatch();


		private readonly CancellationTokenSource cancellation_source = new CancellationTokenSource();

		public CancellationToken Token { get; }

		/// <summary>
		/// Average time this worker remains idle.
		/// The smaller the number, the more work being done.
		/// </summary>
		public long AverageIdleTime {
			get {
				if (idle_stopwatch.IsRunning) {
					if (average_idle_time == 0) {
						return idle_stopwatch.ElapsedMilliseconds;
					}
					return (idle_stopwatch.ElapsedMilliseconds + average_idle_time) / 2;
				}
				return average_idle_time;
			}
		}

		public bool IsIdling => idle_stopwatch.IsRunning;

		public bool IsWorking => work_stopwatch.IsRunning;

		private readonly Action<MqWorker> work;

		public MqWorker(Action<MqWorker> work) {
			this.work = work;
			Token = cancellation_source.Token;
			worker_task = new Task(ProcessQueue, Token, Token, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Start the worker.
		/// </summary>
		public void Start() {
			idle_stopwatch.Start();
			worker_task.Start();
		}

		public void StartIdle() {
			idle_stopwatch.Restart();

			if (work_stopwatch.IsRunning) {
				work_stopwatch.Stop();

				average_work_time = average_idle_time == 0
					? work_stopwatch.ElapsedMilliseconds
					: (work_stopwatch.ElapsedMilliseconds + average_work_time) / 2;
			}
		}

		public void StartWork() {
			work_stopwatch.Restart();
			idle_stopwatch.Stop();

			average_idle_time = average_idle_time == 0
					? idle_stopwatch.ElapsedMilliseconds
					: (idle_stopwatch.ElapsedMilliseconds + average_idle_time) / 2;
		}

		/// <summary>
		/// Interrupt the worker loop and keep the worker in an idle state.
		/// </summary>
		public void Stop() {
			cancellation_source.Cancel();
		}

		private void ProcessQueue(object o) {
			var token = (CancellationToken)o;

			while (token.IsCancellationRequested == false) {
				try {
					work?.Invoke(this);
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