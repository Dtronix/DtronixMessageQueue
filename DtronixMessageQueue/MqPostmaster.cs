using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DtronixMessageQueue {		
	/// <summary>
	/// Postmaster to handle worker creation/deletion and parsing of all incoming and outgoing messages.
	/// </summary>
	public class MqPostmaster<TSession> : IDisposable
		where TSession : MqSession<TSession>, new() {

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
		/// List of all write workers.
		/// </summary>
		private readonly List<MqWorker> write_workers = new List<MqWorker>();

		/// <summary>
		/// Dictionary to prevent multiple reads occurring on the same session concurrently.
		/// </summary>
		private readonly ConcurrentDictionary<MqSession<TSession>, bool> ongoing_read_operations = new ConcurrentDictionary<MqSession<TSession>, bool>();

		/// <summary>
		/// Collection used to hold onto sessions pending read operations.
		/// </summary>
		private readonly BlockingCollection<MqSession<TSession>> read_operations = new BlockingCollection<MqSession<TSession>>();

		/// <summary>
		/// List of all read workers.
		/// </summary>
		private readonly List<MqWorker> read_workers = new List<MqWorker>();

		/// <summary>
		/// Current number of all writer workers.
		/// </summary>
		public int TotalWriters => write_workers.Count;

		/// <summary>
		/// Current number of all reader workers.
		/// </summary>
		public int TotalReaders => read_workers.Count;

		/// <summary>
		/// Configurations for this socket.
		/// </summary>
		private readonly MqSocketConfig config;

		/// <summary>
		/// Creates a new postmaster instance to handle reading and writing of all sessions.
		/// </summary>
		public MqPostmaster(MqSocketConfig config) {
			this.config = config;

			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			supervisor = new MqWorker(SupervisorWork, "postmaster_supervisor");

			// Create one reader and one writer workers to start off with.
			for (int i = 0; i < 2; i++) {
				CreateReadWorker();
				CreateWriteWorker();
			}
			supervisor.Start();
		}

		/// <summary>
		/// Supervisor method to review status of all workers.
		/// Spawns or removes threads depending on workload.
		/// </summary>
		/// <param name="worker">Worker to review.</param>
		private void SupervisorWork(MqWorker worker) {
			while (worker.Token.IsCancellationRequested == false) {
				if (ProcessWorkers(read_workers, config.MaxReadWriteWorkers)) {
					CreateReadWorker();
				}

				if (ProcessWorkers(write_workers, config.MaxReadWriteWorkers)) {
					CreateWriteWorker();
				}
				Thread.Sleep(500);
			}
		}


		/// <summary>
		/// Reviews the specified worker list (reader/writer) and determines if new workers are needed or not.
		/// </summary>
		/// <param name="worker_list">Worker list to review.</param>
		/// <param name="max_workers">Maximum number of workers for this list.</param>
		/// <returns></returns>
		private bool ProcessWorkers(List<MqWorker> worker_list, int max_workers) {
			MqWorker idle_worker = null;
			foreach (var worker in worker_list) {
				if (idle_worker == null && worker.IsIdling) {
					idle_worker = worker;
				}

				// If this is not the idle worker and it has been idle for more than 60 seconds, close down this worker.
				if (worker.IsIdling && worker != idle_worker && worker.AverageIdleTime > config.IdleWorkerTimeout) {
					worker_list.Remove(worker);
					worker.Stop();
				}
			}

			if (max_workers != 0 && worker_list.Count >= max_workers) {
				return false;
			}

			return idle_worker == null;
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


		/// <summary>
		/// Creates a worker writer.
		/// </summary>
		public void CreateWriteWorker() {
			var writer_worker = new MqWorker(worker => {
				MqSession<TSession> session = null;
				bool out_session;

				try {
					worker.StartIdle();
					while (write_operations.TryTake(out session, 60000, worker.Token)) {
						worker.StartWork();

						if (session.ProcessOutbox()) {
							write_operations.TryAdd(session);
						} else {
							ongoing_write_operations.TryRemove(session, out out_session);
						}

						worker.StartIdle();
					}
				} catch (ThreadAbortException) {
				} catch (Exception) {
					if (session != null) {
						/*logger.Error(e,
							is_writer
								? "MqConnection {0}: Exception occurred while when writing."
								: "MqConnection {0}: Exception occurred while when reading.", session.Connection.Id);*/
					}
				}
			}, "mq_write_worker_" + write_workers.Count);

			writer_worker.Start();

			write_workers.Add(writer_worker);
		}

		/// <summary>
		/// Creates a worker reader.
		/// </summary>
		public void CreateReadWorker() {
			var reader_worker = new MqWorker(worker => {
				MqSession<TSession> session = null;
				bool out_session;
				try {
					worker.StartIdle();
					while (read_operations.TryTake(out session, 60000, worker.Token)) {
						worker.StartWork();
						if (session.ProcessIncomingQueue()) {
							read_operations.TryAdd(session);
						} else { 
							ongoing_read_operations.TryRemove(session, out out_session);
						}

						worker.StartIdle();

					}
				} catch (ThreadAbortException) {
				} catch (Exception) {
					if (session != null) {
						/*logger.Error(e,
							is_writer
								? "MqConnection {0}: Exception occurred while when writing."
								: "MqConnection {0}: Exception occurred while when reading.", session.Connection.Id);*/
					}
				}
			}, "mq_read_worker_" + read_workers.Count);

			reader_worker.Start();

			read_workers.Add(reader_worker);
		}

		/// <summary>
		/// Stops all workers and removes all associated resources.
		/// </summary>
		public void Dispose() {
			supervisor.Dispose();
			foreach (var write_worker in write_workers) {
				write_worker.Dispose();
			}

			foreach (var read_worker in read_workers) {
				read_worker.Dispose();
			}
		}
	}
}