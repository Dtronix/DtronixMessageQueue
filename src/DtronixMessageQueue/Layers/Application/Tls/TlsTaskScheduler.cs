using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public sealed class TlsTaskScheduler : TaskScheduler, IDisposable
    {
        private class TaskThread
        {
            public CancellationTokenSource CancellationTokenSource;
            public Thread Thread;
        }

        private readonly BlockingCollection<Task> _tasksCollection = new BlockingCollection<Task>();

        private readonly List<TaskThread> _threads = new List<TaskThread>();

        public TlsTaskScheduler(TlsApplicationConfig config)
        {
            var threads = config.TlsSchedulerThreads;

            if (threads == -1)
                threads = Environment.ProcessorCount;

            for (int i = 0; i < threads; i++)
            {
                var thread = new Thread(Execute)
                {
                    Name = $"TlsTaskScheduler-{i}"
                };

                var taskThread = new TaskThread
                {
                    Thread = thread,
                    CancellationTokenSource = new CancellationTokenSource()
                };

                _threads.Add(taskThread);

                thread.Start(taskThread);
            }
        }

        private void Execute(object thread)
        {
            var taskThread = (TaskThread) thread;
            foreach (var task in _tasksCollection.GetConsumingEnumerable(taskThread.CancellationTokenSource.Token))
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

            foreach (var thread in _threads)
            {
                thread.CancellationTokenSource.Cancel();
                thread.Thread.Abort();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}