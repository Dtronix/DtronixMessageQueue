﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Sequentially processes, balances and gathers metrics on registered actions across multiple threads.
    /// </summary>
    /// <summary>
    /// - Actions registered will be associated with a specific thread and will execute only on that thread
    ///   until the supervisor re-balances the executions.
    /// - Actions may float around between threads while executing to balance out thread execution.
    /// - QueueActionExecution will only queue an action once until it has executed.  Subsequent calls will be ignored.
    /// </summary>
    /// <typeparam name="T">
    /// Type to use for associating with an action.
    /// Usually a value type like Guid or int.
    /// </typeparam>
    public class ActionProcessor<T>
    {

        /// <summary>
        /// Base name for all of the threads to use.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Current threads for this processor to use.
        /// </summary>
        private readonly List<ProcessorThread> _threads;

        /// <summary>
        /// Internal number to keep track of now many threads have been created.
        /// </summary>
        private int _threadId;

        /// <summary>
        /// Timer to execute the supervisor method.
        /// </summary>
        private Timer _supervisorTimer;

        /// <summary>
        /// Actions currently registered with this processor.
        /// </summary>
        private readonly ConcurrentDictionary<T, RegisteredAction> _registeredActions;

        /// <summary>
        /// True if the processor is running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Number of threads currently available to this processor.
        /// </summary>
        public int ThreadCount => _threads.Count;


        /// <summary>
        /// Creates a new processor and threads with the specified thread base name.
        /// Will use the count of processors as the count of threads to create.
        /// </summary>
        /// <param name="name">Base name to associate with this action processor.</param>
        public ActionProcessor(string name) : this(name, Environment.ProcessorCount)
        {
        }

        /// <summary>
        /// Creates a new processor and specified count of threads with the specified thread base name.
        /// </summary>
        /// <param name="name">Base name to associate with this action processor.</param>
        /// <param name="threads">Number of threads to create.</param>
        public ActionProcessor(string name, int threads)
        {
            _name = name;
            _registeredActions = new ConcurrentDictionary<T, RegisteredAction>();

            _threads = new List<ProcessorThread>(threads);

            AddThreads(threads);

            _supervisorTimer = new Timer(Supervise);
        }

        /// <summary>
        /// Adds the specified number of threads to the processor.
        /// </summary>
        /// <param name="count">Number of threads to add.</param>
        public void AddThreads(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var id = Interlocked.Increment(ref _threadId);
                var pThread = new ProcessorThread($"dmq-{_name}-{id}");
                _threads.Add(pThread);
            }
        }


        /// <summary>
        /// Queues the associated action up to execute on the internal thread.
        /// Will only queue the associated action once.  
        /// Subsequent calls will not be queued until first queued action executes.
        /// </summary>
        /// <param name="id">Id associated with an action to execute.</param>
        /// <returns>True if the action was queued.  False if it was not.</returns>
        public bool QueueActionExecution(T id)
        {
            if (_registeredActions.TryGetValue(id, out RegisteredAction processAction))
            {
                if (processAction.QueuedCount < 1)
                {
                    processAction.ProcessorThread.Queue(processAction);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers an action with an associated Id.
        /// </summary>
        /// <param name="id">Id to associate with this action execution.</param>
        /// <param name="action">Action to execute when this Id is queued.</param>
        public void Register(T id, Action action)
        {
            var leastActiveProcessor = _threads.OrderByDescending(pt => pt.IdleTime).First();


            var processAction = new RegisteredAction
            {
                Action = action,
                Id = id,
                ProcessorThread = leastActiveProcessor
            };

            Interlocked.Increment(ref leastActiveProcessor.RegisteredActionsCount);

            if (_registeredActions.TryAdd(id, processAction) == false)
            {
                throw new InvalidOperationException($"Id {id} is already registered.");
            }
        }

        /// <summary>
        /// Remove a registered action from the list.
        /// </summary>
        /// <param name="id"></param>
        public void Deregister(T id)
        {
            if (_registeredActions.TryRemove(id, out RegisteredAction processAction))
            {
                Interlocked.Increment(ref processAction.ProcessorThread.RegisteredActionsCount);
            }
        }

        /// <summary>
        /// Start the processing of all the queues.
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            foreach (var processorThread in _threads)
            {
                processorThread.Start();
            }

            //_supervisorTimer.Change(10000, 10000);
        }

        /// <summary>
        /// Stops the processor and all the threads.  Actions are allowed to continue to queue.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            foreach (var processorThread in _threads)
            {
                processorThread.Stop();
            }

            //_supervisorTimer.Change(-1, -1);
        }

        /// <summary>
        /// Method invoked by a timer to review the current status of the threads and 
        /// re-arrange to balance out long running tasks across multiple threads.
        /// </summary>
        /// <param name="o">Timer reference.</param>
        private void Supervise(object o)
        {
            // TODO: Add logic
        }

        /// <summary>
        /// Contains action and performance info on the performed action.
        /// </summary>
        private class RegisteredAction
        {
            /// <summary>
            /// Id for this action to perform.
            /// </summary>
            public T Id;

            /// <summary>
            /// Action to perform.
            /// </summary>
            public Action Action;

            /// <summary>
            /// Average time in milliseconds it takes for the action to complete.
            /// </summary>
            public float AverageUsageTime;

            /// <summary>
            /// Thread which will execute this action on.
            /// </summary>
            public ProcessorThread ProcessorThread;

            /// <summary>
            /// Integer of the times this action is currently in queue to execute.
            /// </summary>
            public int QueuedCount;
        }


        /// <summary>
        /// Thread manager to handle the stating and stopping of processing threads.
        /// </summary>
        private class ProcessorThread
        {
            /// <summary>
            /// Name of the thread.
            /// </summary>
            private readonly string _name;

            /// <summary>
            /// Thread which is being managed.
            /// </summary>
            private Thread _thread;

            /// <summary>
            /// Actions queued to execute on this thread.
            /// </summary>
            private readonly BlockingCollection<RegisteredAction> _actions;

            /// <summary>
            /// Number of actions queued to execute.
            /// </summary>
            private int _queued;

            /// <summary>
            /// True if the thread is running.  False to cancel the thread inner loop.
            /// </summary>

            /// <summary>
            /// Cancellation source to handle the canceling of the thread loop.
            /// </summary>
            private CancellationTokenSource _cancellationTokenSource;

            /// <summary>
            /// Used to calculate the idle time and the time spend on each action.
            /// </summary>
            private Stopwatch _perfStopwatch;


            /// <summary>
            /// Total actions which this thread can process.
            /// </summary>
            public int RegisteredActionsCount;

            private bool _isRunning;

            /// <summary>
            /// True if the thread is running.  False otherwise.
            /// </summary>
            public bool IsRunning => _isRunning;


            private float _idleTime;

            /// <summary>
            /// Time that this thread has remained idle.  Used for balancing.
            /// </summary>
            public float IdleTime => _idleTime;


            /// <summary>
            /// Starts a new managed thread to handle the actions passed to process.
            /// </summary>
            /// <param name="name">Name of the thread.</param>
            public ProcessorThread(string name)
            {
                _name = name;
                _actions = new BlockingCollection<RegisteredAction>();
            }

            /// <summary>
            /// Internal method called by the newly created thread.
            /// </summary>
            private void ThreadProcess()
            {
                // Restart the stop watch to clear any existing time data.
                _perfStopwatch.Restart();

                // Loop while the cancellation of the thread has not been requested.
                while (IsRunning)
                {
                    while (_actions.TryTake(out RegisteredAction action, 10000, _cancellationTokenSource.Token))
                    {

                        // Decrement the total actions queued and the current action being run.
                        Interlocked.Decrement(ref action.QueuedCount);
                        Interlocked.Decrement(ref _queued);

                        // Update the idle time
                        RollingEstimate(ref _idleTime, _perfStopwatch.ElapsedMilliseconds, 10);

                        // Restart the watch to time the runtime of the action.
                        _perfStopwatch.Restart();

                        // Catch any exceptions and ignore for now.
                        // TODO: Figure out a plan for handling exceptions thrown in the process.
                        try
                        {
                            action.Action();
                        }
                        catch
                        {
                            // ignored
                        }



                        // Add this performance to the estimated rolling average.
                        RollingEstimate(ref action.AverageUsageTime, _perfStopwatch.ElapsedMilliseconds, 10);


                    }
                }

                // Stop the timer as we have existed the main loop.
                _perfStopwatch.Stop();

            }

            /// <summary>
            /// QueueActionExecution a specific process action to execute.
            /// </summary>
            /// <param name="registeredAction">ThreadProcess action to execute.</param>
            public void Queue(RegisteredAction registeredAction)
            {
                Interlocked.Increment(ref _queued);
                Interlocked.Increment(ref registeredAction.QueuedCount);
                _actions.TryAdd(registeredAction);

            }

            /// <summary>
            /// CAncels the loop and stops the thread from running.
            /// </summary>
            public void Stop()
            {
                _cancellationTokenSource.Cancel();
                _isRunning = false;
                _thread = null;
            }

            /// <summary>
            /// Creates a new thread and starts the processor.
            /// </summary>
            public void Start()
            {
                if (_thread != null)
                    throw new InvalidOperationException(
                        "Trying to create a new processor thread when an existing thread is already running.");

                _thread = new Thread(ThreadProcess)
                {
                    Name = _name,
                    IsBackground = true
                };

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

                var fProportion = (float) (uiProportion - 1) / uiProportion;
                rollingEstimate = fProportion * rollingEstimate + 1.0f / uiProportion * update;
            }
        }
    }
}
