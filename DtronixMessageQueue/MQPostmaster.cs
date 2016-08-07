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
		public int MaxFrameSize { get; }
		private readonly MqWorker supervisor;

		public BlockingCollection<MqMailbox> WriteOperations = new BlockingCollection<MqMailbox>();
		private readonly ConcurrentBag<MqWorker> write_workers = new ConcurrentBag<MqWorker>();


		public BlockingCollection<MqMailbox> ReadOperations = new BlockingCollection<MqMailbox>();
		private readonly ConcurrentBag<MqWorker> read_workers = new ConcurrentBag<MqWorker>();

		public MqPostmaster(int max_frame_size) {
			MaxFrameSize = max_frame_size;
			// Add a supervisor to review when it is needed to increase or decrease the worker numbers.
			//supervisor = new MqWorker(SuperviseWorkers);

			// Create one reader and one writer workers to start off with.
			CreateWorker(true);
			CreateWorker(false);
		}


		private async void StartSupervisor() {
			await Task.Delay(2000);
			supervisor.Start();
		}

		private async void SuperviseWorkers(CancellationToken token) {
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
		public void CreateWorker(bool is_writer) {
			var mailbox_collection = is_writer ? WriteOperations : ReadOperations;

			var reader_worker = new MqWorker(token => {
				MqMailbox mailbox = null;

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
				} catch (ThreadAbortException) {
				} catch (Exception e) {
					if (mailbox != null) {
						/*logger.Error(e,
							is_writer
								? "MqConnection {0}: Exception occurred while when writing."
								: "MqConnection {0}: Exception occurred while when reading.", mailbox.Connection.Id);*/
					}
				}
			});

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