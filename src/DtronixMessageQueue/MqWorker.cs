using System;
using System.Diagnostics;
using System.Threading;

namespace DtronixMessageQueue {

	/// <summary>
	/// Worker which creates a new thread and monitor's its usage.
	/// Loops the passed action worker until the worker had been canceled.
	/// </summary>
	public class MqWorker : IDisposable {

		/// <summary>
		/// Average time this worker spent idle.  Used to determine if this worker is still needed.
		/// </summary>
		private long average_idle_time = 0;

		/// <summary>
		/// Average time this worker spent working.  Used to determine if this worker is still needed.
		/// </summary>
		private long average_work_time = 0;

		/// <summary>
		/// Stopwatch used to monitor the idle time.
		/// </summary>
		private readonly Stopwatch idle_stopwatch = new Stopwatch();

		/// <summary>
		/// Stopwatch used to monitor the work time.
		/// </summary>
		private readonly Stopwatch work_stopwatch = new Stopwatch();

		/// <summary>
		/// Token source used to cancel this worker.
		/// </summary>
		private readonly CancellationTokenSource cancellation_source = new CancellationTokenSource();

		/// <summary>
		/// Token used to cancel this worker.
		/// </summary>
		public CancellationToken Token { get; }

		/// <summary>
		/// Average time this worker remains idle.
		/// The smaller the number, the more work being done.
		/// </summary>
		public long AverageIdleTime => average_idle_time;

		/// <summary>
		/// True if this worker is idling.
		/// </summary>
		public bool IsIdling => idle_stopwatch.IsRunning;

		/// <summary>
		/// True if this worker is working.
		/// </summary>
		public bool IsWorking => work_stopwatch.IsRunning;

		/// <summary>
		/// Work action to perform.
		/// </summary>
		private readonly Action<MqWorker> work;

		/// <summary>
		/// Thread this worker uses to perform the work.
		/// </summary>
		private readonly Thread worker_thread;

		/// <summary>
		/// Creates an instance o
		/// </summary>
		/// <param name="work"></param>
		/// <param name="name"></param>
		public MqWorker(Action<MqWorker> work, string name) {
			idle_stopwatch.Start();
			this.work = work;
			Token = cancellation_source.Token;
			worker_thread = new Thread(ProcessQueue) {
				Name = name,
				IsBackground = true,
				Priority = ThreadPriority.Normal
			};
			//worker_task = new Task(ProcessQueue, Token, Token, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Start the worker.
		/// </summary>
		public void Start() {
			worker_thread.Start(this);
			//worker_task.Start();
		}

		/// <summary>
		/// Called by the worker action when the work has completed to signal the worker is idle.
		/// </summary>
		public void StartIdle() {
			idle_stopwatch.Restart();

			if (work_stopwatch.IsRunning) {
				work_stopwatch.Stop();

				average_work_time = average_work_time == 0
					? work_stopwatch.ElapsedMilliseconds
					: (work_stopwatch.ElapsedMilliseconds + average_work_time) / 2;
			}
		}

		/// <summary>
		/// Called by the worker action when the work has started to signal the worker is busy.
		/// </summary>
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

		/// <summary>
		/// Called by the new thread to loop the worker's action.
		/// </summary>
		/// <param name="o"></param>
		private void ProcessQueue(object o) {
			while (Token.IsCancellationRequested == false) {
				try {
					work?.Invoke((MqWorker)o);
				} catch (Exception) {
					// ignored
				}
			}
		}

		/// <summary>
		/// Stops the thread and releases resources.
		/// </summary>
		public void Dispose() {
			if (worker_thread.IsAlive) {
				Stop();
			}

			idle_stopwatch.Stop();
			work_stopwatch.Stop();
			worker_thread.Abort();
		}
	}
}