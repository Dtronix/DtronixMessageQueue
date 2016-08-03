using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DtronixMessageQueue {
	public class MQPostmaster : IDisposable {

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private readonly MQConnector connector;

		private readonly MQWorker supervisor;

		public BlockingCollection<MQMailbox> WriteOperations = new BlockingCollection<MQMailbox>();
		private readonly ConcurrentBag<MQWorker> write_workers = new ConcurrentBag<MQWorker>();


		public BlockingCollection<MQMailbox> ReadOperations = new BlockingCollection<MQMailbox>();
		private readonly ConcurrentBag<MQWorker> read_workers = new ConcurrentBag<MQWorker>();

		public MQPostmaster(MQConnector connector) {
			this.connector = connector;

			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			supervisor = new MQWorker(SuperviseWorkers, connector);

			// Create one reader and one writer workers to start off with.
			CreateWorker(true);
			CreateWorker(false);
			CreateWorker(true);
			CreateWorker(false);

			StartSupervisor();
		}

		private async void StartSupervisor() {
			await Task.Delay(2000);
			supervisor.Start();
		}

		private async void SuperviseWorkers(object o) {
			if (!(o is CancellationToken)) return;

			var token = (CancellationToken)o;
			while (token.IsCancellationRequested == false) {
				if (read_workers.IsEmpty == false) {
					var read_averages = read_workers.Sum(worker => worker.AverageIdleTime)/read_workers.Count;

					if (read_averages < 50) {
						CreateWorker(false);
					}
				}

				if (write_workers.IsEmpty == false) {
					var write_averages = write_workers.Sum(worker => worker.AverageIdleTime)/write_workers.Count;

					if (write_averages < 50) {
						CreateWorker(true);
					}
				}

				await Task.Delay(2000, token);
			}
		}


		/// <summary>
		/// Creates a worker in either a reader or writer state.
		/// </summary>
		/// <param name="is_writer">True if this is a writer worker, false if this is a reader worker.</param>
		private void CreateWorker(bool is_writer) {
			var mailbox_collection = is_writer ? WriteOperations : ReadOperations;

			var reader_worker = new MQWorker(o => {
				var token = (CancellationToken) o;
				MQMailbox mailbox = null;

				try {
					while (mailbox_collection.TryTake(out mailbox, 60000, token)) {
						if (is_writer) {
							// If this is a writer worker, process only the outbox.
							mailbox.ProcessOutbox();

						} else {
							// If this is a reader worker, process only the inbox.
							mailbox.ProcessIncomingQueue();
						}

					}
				} catch (ThreadAbortException e) {
				} catch (Exception e) {
					if (mailbox != null) {
						logger.Error(e,
							is_writer
								? "MQConnection {0}: Exception occurred while when writing."
								: "MQConnection {0}: Exception occurred while when reading.", mailbox.Connection.Id);
					}
				}
			}, connector);

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
