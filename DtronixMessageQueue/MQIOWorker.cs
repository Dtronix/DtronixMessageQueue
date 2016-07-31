using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;

namespace DtronixMessageQueue {
	public class MQIOWorker : IDisposable {
		private class StopWorkerEvent {
		}

		public class WorkerEventArgs : EventArgs {
			public MQIOWorker Worker { get; }
			public CompletionStatus Status { get; }

			public WorkerEventArgs(MQIOWorker worker, CompletionStatus status) {
				Worker = worker;
				Status = status;
			}

		}

		public class WorkerDisconnectEventArgs : EventArgs {
			public MQIOWorker Worker { get; }
			public CompletionStatus Status { get; }
			public string Reason { get; }

			public WorkerDisconnectEventArgs(MQIOWorker worker, CompletionStatus status, string reason) {
				Worker = worker;
				Status = status;
				Reason = reason;
			}

		}

		private Thread worker_thread;
		private CompletionPort completion_port;

		private long average_idle_time = -1;
		private bool busy;

		public bool Busy => busy;

		public event EventHandler<WorkerEventArgs> OnSend;
		public event EventHandler<WorkerEventArgs> OnReceive;
		public event EventHandler<WorkerEventArgs> OnConnect;
		public event EventHandler<WorkerDisconnectEventArgs> OnDisconnect;
		public event EventHandler<WorkerEventArgs> OnSignal;
		public event EventHandler<WorkerEventArgs> OnAccept;

		public MQIOWorker(CompletionPort completion_port) : this(completion_port, "mq-default-worker") {

		}

		public MQIOWorker(CompletionPort completion_port, string thread_name) {
			this.completion_port = completion_port;
			worker_thread = new Thread(ProcessQueue) {
				IsBackground = true,
				Name = thread_name
			};
		}

		/// <summary>
		/// Start the worker.
		/// </summary>
		public void Start() {
			worker_thread.Start();
		}

		/// <summary>
		/// Interrupt the worker loop and keep the worker in an idle state.
		/// </summary>
		public void Stop() {
			completion_port.Signal(new StopWorkerEvent());
		}

		private void ProcessQueue() {
			var cancel = false;
			var idle_stopwatch = new Stopwatch();

			while (!cancel) {
				CompletionStatus completion_status;
				idle_stopwatch.Restart();

				// Check the completion port status
				if (completion_port.GetQueuedCompletionStatus(5000, out completion_status) == false) {
					continue;
				}

				if (completion_status.SocketError != SocketError.Success) {
					completion_status.AsyncSocket.Dispose();
					OnDisconnect?.Invoke(this, new WorkerDisconnectEventArgs(this, completion_status, "Socket error"));
				}

				if (completion_status.BytesTransferred == 0 && completion_status.OperationType == OperationType.Receive) {
					completion_status.AsyncSocket.Dispose();
					OnDisconnect?.Invoke(this, new WorkerDisconnectEventArgs(this, completion_status, "Client disconnected"));
				}

				// If the state is the StopWorkerEvent class, we need to terminate our loop.
				if (completion_status.State is StopWorkerEvent) {
					idle_stopwatch.Stop();
					break;
				}

				// Check the average time this thread remains idle
				average_idle_time = average_idle_time == -1
					? idle_stopwatch.ElapsedMilliseconds
					: (idle_stopwatch.ElapsedMilliseconds + average_idle_time)/2;


				busy = true;
				switch (completion_status.OperationType) {
					case OperationType.Send:
						OnSend?.Invoke(this, new WorkerEventArgs(this, completion_status));
						break;

					case OperationType.Receive:
						OnReceive?.Invoke(this, new WorkerEventArgs(this, completion_status));
						break;

					case OperationType.Connect:
						OnConnect?.Invoke(this, new WorkerEventArgs(this, completion_status));
						break;

					case OperationType.Disconnect:
						completion_status.AsyncSocket.Dispose();
						OnDisconnect?.Invoke(this, new WorkerDisconnectEventArgs(this, completion_status, "Client disconnected"));
						break;

					case OperationType.Signal:
						OnSignal?.Invoke(this, new WorkerEventArgs(this, completion_status));
						break;

					case OperationType.Accept:
						OnAccept?.Invoke(this, new WorkerEventArgs(this, completion_status));
						// Auto accept the next connection.
						completion_status.AsyncSocket.Accept();
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				busy = false;
			}
		}

		public void Dispose() {
			if (worker_thread.IsAlive) {
				Stop();
				worker_thread.Abort();
			}
		}
	}
}
