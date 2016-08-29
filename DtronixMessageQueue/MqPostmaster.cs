using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//using NLog;

namespace DtronixMessageQueue {
	public class MqPostmaster : IDisposable {
		//public int MaxFrameSize { get; }
		private readonly MqWorker supervisor;

		private readonly ConcurrentDictionary<MqSession, bool> ongoing_write_operations = new ConcurrentDictionary<MqSession, bool>();
		private readonly BlockingCollection<MqSession> write_operations = new BlockingCollection<MqSession>();
		private readonly List<MqWorker> write_workers = new List<MqWorker>();

		public int TotalWriters => write_workers.Count;
		public int TotalReaders => read_workers.Count;

		public int MaxWriters { get; set; } = 0;
		public int MaxReaders { get; set; } = 0;

		private readonly ConcurrentDictionary<MqSession, bool> ongoing_read_operations = new ConcurrentDictionary<MqSession, bool>();
		private readonly BlockingCollection<MqSession> read_operations = new BlockingCollection<MqSession>();
		private readonly List<MqWorker> read_workers = new List<MqWorker>();

		public MqPostmaster() {
			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			supervisor = new MqWorker(SupervisorWork, "postmaster_supervisor");

			// Create one reader and one writer workers to start off with.
			for (int i = 0; i < 2; i++) {
				CreateReadWorker();
				CreateWriteWorker();
			}
			supervisor.Start();
		}

		private void SupervisorWork(MqWorker worker) {
			while (worker.Token.IsCancellationRequested == false) {
				if (ProcessWorkers(read_workers, MaxReaders)) {
					CreateReadWorker();
				}

				if (ProcessWorkers(write_workers, MaxWriters)) {
					CreateWriteWorker();
				}
				Thread.Sleep(500);
			}
		}

		public MqPostmaster(MqServer server) {
		}


		public bool SignalWrite(MqSession session) {
			return ongoing_write_operations.TryAdd(session, true) && write_operations.TryAdd(session);
		}

		public bool SignalRead(MqSession session) {
			return ongoing_read_operations.TryAdd(session, true) && read_operations.TryAdd(session);
		}


		private bool ProcessWorkers(List<MqWorker> worker_list, int max_workers) {
			MqWorker idle_worker = null;
			foreach (var worker in worker_list) {
				if (idle_worker == null && worker.IsIdling) {
					idle_worker = worker;
				}

				// If this is not the idle worker and it has been idle for more than 60 seconds, close down this worker.
				if (worker.IsIdling && worker != idle_worker && worker.AverageIdleTime > 60000) {
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
		/// Creates a worker writer.
		/// </summary>
		public void CreateWriteWorker() {
			var writer_worker = new MqWorker(worker => {
				MqSession session = null;
				bool out_session;

				try {
					worker.StartIdle();
					while (write_operations.TryTake(out session, 60000, worker.Token)) {
						worker.StartWork();
						session.ProcessOutbox();
						worker.StartIdle();
						ongoing_write_operations.TryRemove(session, out out_session);

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
				MqSession session = null;
				bool out_session;
				try {
					worker.StartIdle();
					while (read_operations.TryTake(out session, 60000, worker.Token)) {
						worker.StartWork();
						session.ProcessIncomingQueue();
						worker.StartIdle();
						ongoing_read_operations.TryRemove(session, out out_session);

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