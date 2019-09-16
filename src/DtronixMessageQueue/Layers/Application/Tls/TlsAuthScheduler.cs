using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public sealed class TlsAuthScheduler : TaskScheduler, IDisposable

    {
        private readonly BlockingCollection<Task> _tasksCollection = new BlockingCollection<Task>();

        private readonly Thread _mainThread;

        public TlsAuthScheduler()
        {
            _mainThread = new Thread(Execute);
            if (!_mainThread.IsAlive)
            {
                _mainThread.Start();
            }
        }

        private void Execute()
        {
            foreach (var task in _tasksCollection.GetConsumingEnumerable())
                TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasksCollection.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            _tasksCollection.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            _tasksCollection.CompleteAdding();
            _tasksCollection.Dispose();
            _mainThread.Abort();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}