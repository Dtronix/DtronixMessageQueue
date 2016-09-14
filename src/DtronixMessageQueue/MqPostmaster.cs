using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Amib.Threading;

namespace DtronixMessageQueue {
	/// <summary>
	/// Postmaster to handle worker creation/deletion and parsing of all incoming and outgoing messages.
	/// </summary>
	public class MqPostmaster<TSession> : IDisposable
		where TSession : MqSession<TSession>, new() {

		private SmartThreadPool thread_pool;

		private class WorkerInfo {

			public enum WorkerType {
				Reader,
				Writer
			}
			public BlockingCollection<MqSession<TSession>> Operations;
			public ConcurrentDictionary<MqSession<TSession>, bool> OngoingOperations;
			public WorkerType Type;
		}

		/// <summary>
		/// Internal worker to review the current work being done.
		/// </summary>
		private readonly MqWorker supervisor;

		/// <summary>
		/// Dictionary to prevent multiple writes occurring on the same session concurrently.
		/// </summary>
		private readonly ConcurrentDictionary<MqSession<TSession>, bool> ongoing_write_operations = new ConcurrentDictionary<MqSession<TSession>, bool>();

		/// <summary>
		/// Collection used to hold onto sessions pending write operations.
		/// </summary>
		private readonly BlockingCollection<MqSession<TSession>> write_operations = new BlockingCollection<MqSession<TSession>>();

		/// <summary>
		/// Dictionary to prevent multiple reads occurring on the same session concurrently.
		/// </summary>
		private readonly ConcurrentDictionary<MqSession<TSession>, bool> ongoing_read_operations = new ConcurrentDictionary<MqSession<TSession>, bool>();

		/// <summary>
		/// Collection used to hold onto sessions pending read operations.
		/// </summary>
		private readonly BlockingCollection<MqSession<TSession>> read_operations = new BlockingCollection<MqSession<TSession>>();

		/// <summary>
		/// Configurations for this socket.
		/// </summary>
		private readonly MqSocketConfig config;

		private CancellationTokenSource cancellation_token_source = new CancellationTokenSource();

		/// <summary>
		/// Creates a new postmaster instance to handle reading and writing of all sessions.
		/// </summary>
		public MqPostmaster(MqSocketConfig config) {
			this.config = config;

			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			//supervisor = new MqWorker(SupervisorWork, "postmaster_supervisor");

			// Create one reader and one writer workers to start off with.
			


			//supervisor.Start();

			thread_pool = new SmartThreadPool(config.IdleWorkerTimeout, config.MaxReadWriteWorkers);

			var writer_info = new WorkerInfo {
				Type = WorkerInfo.WorkerType.Writer,
				OngoingOperations = ongoing_write_operations,
				Operations = write_operations
			};

			thread_pool.QueueWorkItem(ProcessReadWrite, writer_info, cancellation_token_source.Token);


			var reader_info = new WorkerInfo {
				Type = WorkerInfo.WorkerType.Reader,
				OngoingOperations = ongoing_read_operations,
				Operations = read_operations
			};

			thread_pool.QueueWorkItem(ProcessReadWrite, reader_info, cancellation_token_source.Token);
		}

		/// <summary>
		/// Signals the postmaster that a data is ready to be sent on the specified session.
		/// </summary>
		/// <param name="session">Session to send data on.</param>
		/// <returns>True if write was queued.  False if the write action was already queued.</returns>
		public bool SignalWrite(MqSession<TSession> session) {
			return ongoing_write_operations.TryAdd(session, true) && write_operations.TryAdd(session);
		}


		/// <summary>
		/// Signals the postmaster that a data is ready to be read on the specified session.
		/// </summary>
		/// <param name="session">Session to read from.</param>
		/// <returns>True if write was queued.  False if the read action was already queued.</returns>
		public bool SignalRead(MqSession<TSession> session) {
			return ongoing_read_operations.TryAdd(session, true) && read_operations.TryAdd(session);
		}


		private void ProcessReadWrite(WorkerInfo info, CancellationToken token) {
			MqSession<TSession> session = null;
			var more_work = false;
			do {
				// Only queue this item up one time per method call.
				if (more_work == false) {
					info.Operations.TryTake(out session, 60000, token);
					thread_pool.QueueWorkItem(ProcessReadWrite, info, token);
				}

				if (info.Type == WorkerInfo.WorkerType.Writer ? session.ProcessOutbox() : session.ProcessIncomingQueue()) {
					more_work = true;
					continue;
				}

				more_work = false;

				bool out_session;
				info.OngoingOperations.TryRemove(session, out out_session);
			} while (more_work);
		}


		/// <summary>
		/// Stops all workers and removes all associated resources.
		/// </summary>
		public void Dispose() {
			thread_pool.Cancel(true);
			thread_pool.Dispose();
		}
	}
}