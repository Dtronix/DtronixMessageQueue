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

		private readonly ConcurrentDictionary<MqMailbox, bool> ongoing_write_operations = new ConcurrentDictionary<MqMailbox, bool>();
		private readonly BlockingCollection<MqMailbox> write_operations = new BlockingCollection<MqMailbox>();
		private readonly List<MqWorker> write_workers = new List<MqWorker>();


		private readonly ConcurrentDictionary<MqMailbox, bool> ongoing_read_operations = new ConcurrentDictionary<MqMailbox, bool>();
		private readonly BlockingCollection<MqMailbox> read_operations = new BlockingCollection<MqMailbox>();
		private readonly List<MqWorker> read_workers = new List<MqWorker>();

		public MqPostmaster() {
			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			supervisor = new MqWorker(SupervisorWork, "postmaster_supervisor");

			// Create one reader and one writer workers to start off with.
			CreateReadWorker();
			CreateWriteWorker();


			supervisor.Start();
		}

		private void SupervisorWork(MqWorker worker) {
			while (worker.Token.IsCancellationRequested == false) {
				if (ProcessWorkers(read_workers)) {
					CreateReadWorker();
				}

				if (ProcessWorkers(write_workers)) {
					CreateWriteWorker();
				}
				Thread.Sleep(500);
			}
		}

		public MqPostmaster(MqServer server) {
		}


		public bool SignalWrite(MqMailbox mailbox) {
			return ongoing_write_operations.TryAdd(mailbox, true) && write_operations.TryAdd(mailbox);
		}

		public bool SignalRead(MqMailbox mailbox) {
			return ongoing_read_operations.TryAdd(mailbox, true) && read_operations.TryAdd(mailbox);
		}


		private bool ProcessWorkers(List<MqWorker> worker_list) {
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

			return idle_worker == null;
		}


		/// <summary>
		/// Creates a worker writer.
		/// </summary>
		public void CreateWriteWorker() {
			Console.WriteLine("Created write worker.");
			var writer_worker = new MqWorker(worker => {
				MqMailbox mailbox = null;
				bool out_mailbox;

				try {
					worker.StartIdle();
					while (write_operations.TryTake(out mailbox, 60000, worker.Token)) {
						worker.StartWork();
						mailbox.ProcessOutbox();
						worker.StartIdle();
						ongoing_write_operations.TryRemove(mailbox, out out_mailbox);

					}
				} catch (ThreadAbortException) {
				} catch (Exception) {
					if (mailbox != null) {
						/*logger.Error(e,
							is_writer
								? "MqConnection {0}: Exception occurred while when writing."
								: "MqConnection {0}: Exception occurred while when reading.", mailbox.Connection.Id);*/
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
			Console.WriteLine("Created read worker.");
			var reader_worker = new MqWorker(worker => {
				MqMailbox mailbox = null;
				bool out_mailbox;
				try {
					worker.StartIdle();
					while (read_operations.TryTake(out mailbox, 60000, worker.Token)) {
						worker.StartWork();
						mailbox.ProcessIncomingQueue();
						worker.StartIdle();
						ongoing_read_operations.TryRemove(mailbox, out out_mailbox);

					}
				} catch (ThreadAbortException) {
				} catch (Exception) {
					if (mailbox != null) {
						/*logger.Error(e,
							is_writer
								? "MqConnection {0}: Exception occurred while when writing."
								: "MqConnection {0}: Exception occurred while when reading.", mailbox.Connection.Id);*/
					}
				}
			}, "mq_read_worker_" + read_workers.Count);

			reader_worker.Start();

			read_workers.Add(reader_worker);
		}

		public void Dispose() {
			supervisor.Stop();
			foreach (var write_worker in write_workers) {
				write_worker.Stop();
			}

			foreach (var read_worker in read_workers) {
				read_worker.Stop();
			}
		}
	}
}