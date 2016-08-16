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
		private readonly ConcurrentDictionary<MqWorker, bool> write_workers = new ConcurrentDictionary<MqWorker, bool>();


		private readonly ConcurrentDictionary<MqMailbox, bool> ongoing_read_operations = new ConcurrentDictionary<MqMailbox, bool>();
		private readonly BlockingCollection<MqMailbox> read_operations = new BlockingCollection<MqMailbox>();
		private readonly ConcurrentDictionary<MqWorker, bool> read_workers = new ConcurrentDictionary<MqWorker, bool>();

		public MqPostmaster() {
			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			supervisor = new MqWorker(SuperviseWorkers);

			// Create one reader and one writer workers to start off with.
			CreateReadWorker();
			CreateWriteWorker();


			supervisor.Start();
		}


		public bool SignalWrite(MqMailbox mailbox) {
			return ongoing_write_operations.TryAdd(mailbox, true) && write_operations.TryAdd(mailbox);
		}

		public bool SignalRead(MqMailbox mailbox) {
			return ongoing_read_operations.TryAdd(mailbox, true) && read_operations.TryAdd(mailbox);
		}


		private async void SuperviseWorkers(MqWorker worker) {
			while (worker.Token.IsCancellationRequested == false) {

				if (ProcessWorkers(read_workers.Keys.ToArray(), read_workers)) {
					CreateReadWorker();
					Console.WriteLine("Created read worker.");
				}

				if (ProcessWorkers(write_workers.Keys.ToArray(), write_workers)) {
					CreateWriteWorker();
					
				}

				await Task.Delay(500, worker.Token);
			}
		}


		private bool ProcessWorkers(MqWorker[] workers, ConcurrentDictionary<MqWorker, bool> worker_dict) {
			MqWorker idle_worker = null;
			foreach (var worker in workers) {
				if (worker.IsIdling) {
					idle_worker = worker;
				}

				// If this is not the idle worker and it has been idle for more than 60 seconds, close down this worker.
				if (worker.IsIdling && worker != idle_worker && worker.AverageIdleTime > 60000) {
					bool out_worker;
					worker_dict.TryRemove(worker, out out_worker);
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

				try {
					worker.StartIdle();
					while (write_operations.TryTake(out mailbox, 60000, worker.Token)) {
						worker.StartWork();
						mailbox.ProcessOutbox();
						bool out_mailbox;
						ongoing_write_operations.TryRemove(mailbox, out out_mailbox);
						worker.StartIdle();
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
			});

			writer_worker.Start();

			write_workers.TryAdd(writer_worker, true);
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
						
						ongoing_read_operations.TryRemove(mailbox, out out_mailbox);
						worker.StartIdle();
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
			});

			reader_worker.Start();

			read_workers.TryAdd(reader_worker, true);
		}

		public void Dispose() {
			supervisor.Stop();
			foreach (var write_worker in write_workers) {
				write_worker.Key.Stop();
			}

			foreach (var read_worker in read_workers) {
				read_worker.Key.Stop();
			}
		}
	}
}