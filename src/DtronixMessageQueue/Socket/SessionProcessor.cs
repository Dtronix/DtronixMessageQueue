using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Handles all inbox and outbox queue processing 
    /// </summary>
    public class SessionProcessor<T>
    {
        private readonly string _name;
        private readonly List<ProcessorThread> _activeThreads;
        private readonly ConcurrentDictionary<T, ProcessorThread> _registeredSessions;

        public bool IsRunning { get; set; }


        public SessionProcessor(string name) : this(Environment.ProcessorCount, name)
        {

        }

        public SessionProcessor(int threads, string name)
        {
            _name = name;
            _registeredSessions = new ConcurrentDictionary<T, ProcessorThread>();

            _activeThreads = new List<ProcessorThread>(threads);
            for (int i = 0; i < threads; i++)
            {
                var pthread = new ProcessorThread($"dmq-{_name}-{i}");
                _activeThreads.Add(pthread);
            }
        }

        public void QueueProcess(ref T id, Action action)
        {
            if (_registeredSessions.TryGetValue(id, out ProcessorThread processor))
            {
                processor.QueueProcess(ref id, action);
            }
        }

        public void Register(ref T id)
        {
            var leastActive = _activeThreads.OrderByDescending(pt => pt.IdleTime).First();
            leastActive.Register(ref id);
            _registeredSessions.TryAdd(id, leastActive);
        }

        public void Unregister(ref T id)
        {
            if (_registeredSessions.TryRemove(id, out ProcessorThread pthread))
            {
                pthread.Unregister(ref id);
            }
        }

        public void Start()
        {
            IsRunning = true;
            foreach (var processorThread in _activeThreads)
            {
                processorThread.Start();
            }
        }

        public void Stop()
        {
            IsRunning = false;
            foreach (var processorThread in _activeThreads)
            {
                processorThread.Stop();
            }
        }

        private class ProcessAction
        {
            public T Guid { get; set; }
            public Action Action { get; set; }
        }


        private class ProcessorThread
        {
            private readonly Thread _thread;
            private readonly BlockingCollection<ProcessAction> _actions;
            private int _queued;
            private bool _isRunning;
            private CancellationTokenSource _cancellationTokenSource;
            private Stopwatch _perfStopwatch;
            private float _idleTime;

            private readonly ConcurrentDictionary<T, float> _guidPerformance;

            private int _registeredGuids;




            public int Queued => _queued;

            public bool IsRunning => _isRunning;

            public int RegisteredGuids => _registeredGuids;

            public float IdleTime => _idleTime;

            public ProcessorThread(string name)
            {
                _queued = 0;
                _thread = new Thread(Process);
                _thread.Name = name;
                _actions = new BlockingCollection<ProcessAction>();
                _guidPerformance = new ConcurrentDictionary<T, float>();
            }

            private void Process()
            {
                _perfStopwatch.Restart();
                while (IsRunning)
                {
                    while (_actions.TryTake(out ProcessAction action, 10000, _cancellationTokenSource.Token))
                    {

                        Interlocked.Decrement(ref _queued);
                        // Update the idle time
                        RollingEstimate(ref _idleTime, _perfStopwatch.ElapsedMilliseconds, 10);

                        _perfStopwatch.Restart();

                        // Perform action
                        action.Action();


                        // Add this performance to the estimated rolling average.
                        if (_guidPerformance.TryGetValue(action.Guid, out float previousAverage))
                        {
                            RollingEstimate(ref previousAverage, _perfStopwatch.ElapsedMilliseconds, 10);
                        }

                    }
                }
                _perfStopwatch.Stop();

            }

            public void QueueProcess(ref T id, Action action)
            {
                Interlocked.Increment(ref _queued);

                _actions.TryAdd(new ProcessAction
                {
                    Guid = id,
                    Action = action
                });

            }

            public void Register(ref T id)
            {
                if (_guidPerformance.TryAdd(id, 0))
                {
                    Interlocked.Increment(ref _registeredGuids);
                }
            }

            public void Unregister(ref T id)
            {
                float perfValue;
                if (_guidPerformance.TryRemove(id, out perfValue))
                {
                    Interlocked.Decrement(ref _registeredGuids);
                }
            }

            public void Stop()
            {
                _cancellationTokenSource.Cancel();
                _isRunning = false;
            }

            public void Start()
            {
                _perfStopwatch = new Stopwatch();
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
                _thread.Start();
                
            }

            /// <summary>
            /// Function to keep an estimate similar to a moving average.
            /// </summary>
            /// <param name="rollingEstimate">Previous estimate of the value.</param>
            /// <param name="update">New value to update against the estimate.</param>
            /// <param name="uiProportion">
            /// The proportion of the previous value / new value
            /// 0 & 1 = no averaging
            /// 2 = 1/2 * Avg + 1/2 * New, 3 = 2/3 * Avg + 1/3 * New,
            /// 10 = 9/10 * Avg + 1 / 10 * New etc....</param>
            /// <returns></returns>
            /// <remarks>
            /// Adam Fullerton
            /// https://www.codeproject.com/Articles/17860/A-Simple-Moving-Average-Algorithm?msg=5040037#xx5040037xx
            /// </remarks>
            private void RollingEstimate(ref float rollingEstimate, float update, int uiProportion)
            {
                if (uiProportion <= 1)
                    rollingEstimate = update;

                float fProportion = (float)(uiProportion - 1) / uiProportion;
                rollingEstimate = fProportion * rollingEstimate + 1.0f / uiProportion * update;
            }
        }
    }
}
