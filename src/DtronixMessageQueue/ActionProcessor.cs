using System;
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
    /// - QueueOnce will only queue an action once until it has executed.  Subsequent calls will be ignored.
    /// </summary>
    /// <typeparam name="T">
    /// Type to use for associating with an action.
    /// Usually a value type like Guid or int.
    /// </typeparam>
    public class ActionProcessor<T>
    {
        private readonly Config _configs;

        /// <summary>
        /// Ordering when selecting a processor thread.
        /// </summary>
        public enum SortOrder
        {
            /// <summary>
            /// Selects the processor with the most registered actions.
            /// </summary>
            MostRegistered,

            /// <summary>
            /// Selects the processor with the least registered actions.
            /// </summary>
            LeastRegistered
        }

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
        private readonly Timer _supervisorTimer;

        /// <summary>
        /// Actions currently registered with this processor.
        /// </summary>
        private readonly ConcurrentDictionary<T, RegisteredAction> _registeredActions;

        /// <summary>
        /// Lock to ensure that two threads to re-balance at the same time.
        /// </summary>
        private readonly object _rebalanceLock = new object();

        private bool _isRunning;

        /// <summary>
        /// True if the processor is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Number of threads currently available to this processor.
        /// </summary>
        public int ThreadCount => _threads.Count;

        /// <summary>
        /// Internally called event used for testing.
        /// Called when the processor has completed all queued items.
        /// </summary>
        private readonly Action<ActionProcessor<T>> _onComplete;

        /// <summary>
        /// Creates a new processor and with the specified configurations.
        /// </summary>
        /// <param name="configs">Configurations to apply</param>
        public ActionProcessor(Config configs) : this(configs, null)
        {
            
        }

        /// <summary>
        /// Creates a new processor and with the specified configurations.
        /// </summary>
        /// <param name="configs">Configurations to apply</param>
        /// <param name="onComplete">Method called when all actions have been processed and the threads are idle.</param>
        internal ActionProcessor(Config configs, Action<ActionProcessor<T>> onComplete)
        {
            _configs = configs;
            _registeredActions = new ConcurrentDictionary<T, RegisteredAction>();
            _onComplete = onComplete;

            _threads = new List<ProcessorThread>(configs.StartThreads);

            // Add the specified number of threads.
            AddThread(configs.StartThreads);

            // Create the supervisor timer.
            _supervisorTimer = new Timer(Supervise);
        }

        /// <summary>
        /// Adds the specified number of threads to the processor.
        /// </summary>
        /// <param name="count">Number of threads to add.</param>
        public void AddThread(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var id = Interlocked.Increment(ref _threadId);
                var pThread = new ProcessorThread($"dmq-{_configs.ThreadName}-{id}");
                _threads.Add(pThread);
                if (_isRunning)
                    pThread.Start();

                // Bind the idle event if it is not null for testing purposes.
                if (_onComplete != null)
                    pThread.Idle = e => InvokeIfComplete();
            }

            // Re-balance the load if we are running
            if (_isRunning)
                RebalanceLoad(false);
        }

        /// <summary>
        /// Removes the least active thread and moves all queued actions to the next least active thread.
        /// Requires at least 2 threads to run.
        /// </summary>
        /// <param name="count">Number of threads to remove</param>
        public void RemoveThread(int count)
        {
            if (_threads.Count < 2)
                throw new InvalidOperationException(
                    $"Can not remove {count} threads.  Must maintain at least one thread for execution.");

            if (_threads.Count - count < 1)
                throw new ArgumentException(
                    $"Can not remove {count} threads since this would leave no threads running.",
                    nameof(count));

            // Create a dictionary of ids and actions to re-queue once they have been placed in their final locations.
            var reQueueActions = new Dictionary<T, RegisteredAction>();

            for (var i = 0; i < count; i++)
            {
                // Remove the least active thread from the list of active threads.
                var leastActiveProcessor = GetProcessorThread(SortOrder.LeastRegistered);
                _threads.Remove(leastActiveProcessor);

                // Get the next least active processor thread.
                var nextLeastActiveProcessor = GetProcessorThread(SortOrder.LeastRegistered);
                leastActiveProcessor.Stop();

                var registeredActions = leastActiveProcessor.GetActions(-1);

                // Transfer the queues from one thread to the other.
                foreach (var registeredAction in registeredActions)
                {
                    // Transfer the action to the next least used thread.
                    TransferAction(registeredAction, nextLeastActiveProcessor);

                    if (!reQueueActions.ContainsKey(registeredAction.Id))
                        reQueueActions.Add(registeredAction.Id, registeredAction);
                }
            }

            // Re-queue all the actions only once after they have moved around.
            foreach (var registeredAction in reQueueActions)
            {
                for (int i = 0; i < registeredAction.Value.QueuedCount; i++)
                {
                    registeredAction.Value.ProcessorThread.Queue(registeredAction.Value, true);
                }

                // Reset the queue to allow for adding again.
                registeredAction.Value.TransferLock.Reset();
            }

            // Re-balance the load if it is warranted.
            if (ShouldRebalance())
                RebalanceLoad(false);
        }


        /// <summary>
        /// Method invoked by a timer to review the current status of the threads and 
        /// re-arrange to balance out long running tasks across multiple threads.
        /// </summary>
        /// <param name="o">Timer reference.</param>
        private void Supervise(object o)
        {
            if (ShouldRebalance())
                RebalanceLoad(true);
        }

        /// <summary>
        /// Returns true if the loading of the actions is unbalanced.
        /// </summary>
        /// <returns>True if the load should be rebalanced.</returns>
        private bool ShouldRebalance()
        {
            var mostRegistered = _threads.OrderByDescending(pt => pt.RegisteredActionsCount).FirstOrDefault();
            var leastRegistered = _threads.OrderBy(pt => pt.RegisteredActionsCount).FirstOrDefault();

            // No threads are running.
            if (mostRegistered == null || leastRegistered == null)
                return false;

            // If these are equal, nothing to do.
            if (mostRegistered == leastRegistered)
                return false;

            // Get the difference and if the min and max number of registrations is greater than 10, time to re-balance.
            return Math.Abs(mostRegistered.RegisteredActionsCount - leastRegistered.RegisteredActionsCount) >
                   _configs.RebalanceLoadDelta;
        }

        /// <summary>
        /// Re-balances all the actions based upon their usage.
        /// Distributes most active to least active round-robin style to all the threads.
        /// </summary>
        /// <param name="isSupervisor">True if this is being called from the supervisor thread.  False otherwise.</param>
        private void RebalanceLoad(bool isSupervisor)
        {
            lock (_rebalanceLock)
            {
                var actions = _registeredActions.Select(kvp => kvp.Value)
                    .OrderByDescending(ra => ra.AverageUsageTime)
                    .ToArray();
                var totalActions = actions.Length;

                // Cache the total threads.
                var threads = _threads.ToArray();
                var totalThreads = threads.Length;
                var currentThreadNumber = 0;

                for (var i = 0; i < totalActions; i++)
                {
                    // If the current thread is not active, get a new array of threads and reset to the beginning.
                    if (threads[currentThreadNumber].IsRunning == false)
                    {
                        threads = _threads.ToArray();
                        totalThreads = threads.Length;
                        currentThreadNumber = 0;
                        i--;

                        // If there is only one thread, and it is not running, quit the balancing since there is nothing to do.
                        if (threads.Length == 1 && threads[0].IsRunning == false)
                            return;

                        continue;
                    }
                    // Round robin actions to all active threads.
                    TransferAction(actions[i], threads[currentThreadNumber]);

                    // Reset to the beginning if we are at the end.
                    if (++currentThreadNumber >= totalThreads)
                        currentThreadNumber = 0;
                }

                // Reset the supervisor to the default timespan again if this is not being called from the supervisor.
                if (_isRunning && !isSupervisor)
                    _supervisorTimer.Change(_configs.RebalanceLoadPeriod, _configs.RebalanceLoadPeriod);
            }
        }

        /// <summary>
        /// Stops and transfers all actions from one thread to a specified thread.
        /// </summary>
        /// <param name="registeredAction">Action to transfer to another processor thread.</param>
        /// <param name="destination">Thread to move all the actions to.</param>
        private static void TransferAction(RegisteredAction registeredAction, ProcessorThread destination)
        {
            // If we are moving to the same destination that it is already on, do nothing.
            if (registeredAction.ProcessorThread == destination)
                return;

            registeredAction.ProcessorThread.DeregisterAction(registeredAction);
            destination.RegisterAction(registeredAction);

            // Set the queue to continue to allow adds.
            registeredAction.TransferLock.Set();
        }


        /// <summary>
        /// Queues the associated action up to execute on the internal thread.
        /// Will only queue the associated action once.  
        /// Subsequent calls will not be queued until first queued action executes.
        /// </summary>
        /// <param name="id">Id associated with an action to execute.</param>
        /// <returns>True if the action was queued.  False if it was not.</returns>
        public bool QueueOnce(T id)
        {
            return Queue(id, 1);
        }

        /// <summary>
        /// Queues the associated action up to execute on the internal thread.
        /// </summary>
        /// <param name="id">Id associated with an action to execute.</param>
        /// <returns>True if the action was queued.  False if it was not.</returns>
        public bool Queue(T id)
        {
            return Queue(id, -1);
        }

        /// <summary>
        /// Queues the associated action up to execute on the internal thread.
        /// Will only queue the associated action once.  
        /// Subsequent calls will not be queued until first queued action executes.
        /// </summary>
        /// <param name="id">Id associated with an action to execute.</param>
        /// <param name="limit">Limits the number of times this item can be queued to execute.  -1 for unlimited.</param>
        /// <returns>True if the action was queued.  False if it was not.</returns>
        private bool Queue(T id, int limit)
        {
            if (_registeredActions.TryGetValue(id, out RegisteredAction processAction))
            {
                // If the processor thread is not running, then it is most likely in the process of transferring to another thread.
                if (processAction.ProcessorThread.IsRunning == false)
                    throw new InvalidOperationException("Tried to queue an execute on a processor thread not running.");

                if (limit == -1 || processAction.QueuedCount < limit)
                {
                    processAction.ProcessorThread.Queue(processAction, false);
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
            var leastActiveProcessor = GetProcessorThread(SortOrder.LeastRegistered);

            var processAction = new RegisteredAction
            {
                Action = action,
                Id = id
            };

            leastActiveProcessor.RegisterAction(processAction);

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
                processAction?.ProcessorThread.DeregisterAction(processAction);
            }
        }

        /// <summary>
        /// Retrieves action by a set ID.  Used for testing only.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal RegisteredAction GetActionById(T id)
        {
            return _registeredActions[id];
        }

        /// <summary>
        /// Internal method called for testing when the processors are complete.
        /// </summary>
        private void InvokeIfComplete()
        {
            if (_threads.Sum(thread => thread.Queued) == 0)
                _onComplete?.Invoke(this);
        }

        /// <summary>
        /// Gets a single processor thread based upon with specified conditions.
        /// </summary>
        /// <param name="order">Specifies to retrieve the most or least action registered processor.</param>
        /// <returns>Processor thread.</returns>
        private ProcessorThread GetProcessorThread(SortOrder order)
        {
            var processorSorter = _threads.Where(pt => pt.IsRunning);

            var selectedProcessor = order == SortOrder.LeastRegistered
                ? processorSorter.OrderBy(pt => pt.RegisteredActionsCount).FirstOrDefault()
                : processorSorter.OrderByDescending(pt => pt.RegisteredActionsCount).FirstOrDefault();

            if (selectedProcessor == null)
                throw new InvalidOperationException("No ProcessorThreads are running.");

            return selectedProcessor;
        }

        /// <summary>
        /// Start the processing of all the queues.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            foreach (var processorThread in _threads)
            {
                processorThread.Start();

                // Bind the idle event if it is not null for testing purposes.
                if (_onComplete != null)
                    processorThread.Idle = e => InvokeIfComplete();
            }

            _supervisorTimer.Change(_configs.RebalanceLoadPeriod, _configs.RebalanceLoadPeriod);


        }

        /// <summary>
        /// Stops the processor and all the threads.  Actions are allowed to continue to queue.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            foreach (var processorThread in _threads)
                processorThread.Stop();

            _supervisorTimer.Change(-1, -1);
        }

        /// <summary>
        /// Configurations for the processor.
        /// </summary>
        public class Config
        {
            /// <summary>
            /// Base name for all the created threads.
            /// </summary>
            public string ThreadName { get; set; }

            /// <summary>
            /// Initial number of threads to start.
            /// Defaults to the number of logical processors
            /// </summary>
            public int StartThreads { get; set; } = Environment.ProcessorCount;

            /// <summary>
            /// Number of milliseconds for this processor to automatically re-balance any registered actions if required.
            /// Default is 10 seconds.
            /// </summary>
            public int RebalanceLoadPeriod { get; set; } = 10000;

            /// <summary>
            /// Number used to determine when re-balancing needs to occur.
            /// When the difference between the thread with the max registered actions
            /// and thread with the min number of registered actions is greater than this number,
            /// a re-balancing will take place.
            /// Default is 10.
            ///
            /// </summary>
            public int RebalanceLoadDelta { get; set; } = 10;
        }

        /// <summary>
        /// Contains action and performance info on the performed action.
        /// </summary>
        internal class RegisteredAction
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

            /// <summary>
            /// Called before adding this action to a queue to prevent
            /// queuing in an inactive thread.
            /// </summary>
            public ManualResetEventSlim TransferLock = new ManualResetEventSlim(true);

            /// <summary>
            /// Integer of the total number of exceptions this action has thrown.
            /// </summary>
            public int ThrownExceptionsCount;
        }


        /// <summary>
        /// Thread manager to handle the stating and stopping of processing threads.
        /// </summary>
        internal class ProcessorThread
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
            internal int Queued;

            /// <summary>
            /// Cancellation source to handle the canceling of the thread loop.
            /// </summary>
            private CancellationTokenSource _cancellationTokenSource;

            /// <summary>
            /// Used to calculate the idle time and the time spend on each action.
            /// </summary>
            private Stopwatch _perfStopwatch;

            /// <summary>
            /// Contains all the actions registered on this thread.
            /// </summary>
            private readonly ConcurrentDictionary<T, RegisteredAction> _registeredActions;

            private int _registeredActionsCount;

            /// <summary>
            /// Total actions which this thread is registered to process.
            /// </summary>
            public int RegisteredActionsCount => _registeredActionsCount;

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
            /// Internal event called for testing when this thread has completed all registered work.
            /// </summary>
            public Action<ProcessorThread> Idle;

            /// <summary>
            /// Starts a new managed thread to handle the actions passed to process.
            /// </summary>
            /// <param name="name">Name of the thread.</param>
            public ProcessorThread(string name)
            {
                _name = name;
                _actions = new BlockingCollection<RegisteredAction>();
                _registeredActions = new ConcurrentDictionary<T, RegisteredAction>();
            }

            /// <summary>
            /// Internal method called by the newly created thread.
            /// </summary>
            private void ThreadProcess()
            {
                RegisteredAction action = null;
                // Restart the stop watch to clear any existing time data.
                _perfStopwatch.Restart();

                // Loop while the cancellation of the thread has not been requested.
                while (_isRunning)
                {
                    // Update the idle time
                    RollingEstimate(ref _idleTime, _perfStopwatch.ElapsedMilliseconds, 10);

                    // Wrap the entire loop in a try/catch for when the loop times out, it will not throw.
                    try
                    {
                        while (_actions.TryTake(out action, 1000, _cancellationTokenSource.Token))
                        {
                            // If this action is not registered to this thread, add it back to be
                            // processed again at a later time in hopes that the processor thread will be finally
                            // set to an active processor.
                            if (action.ProcessorThread == null)
                            {
                                _actions.Add(action);
                                continue;
                            }

                            // Decrement the total actions queued.
                            Interlocked.Decrement(ref Queued);

                            // If this action was transferred to another thread, queue the action up on that other thread.
                            if (action.ProcessorThread != this)
                            {
                                action.ProcessorThread.Queue(action, true);
                                continue;
                            }

                            // Decrement only if this is going to run on this thread.
                            Interlocked.Decrement(ref action.QueuedCount);

                            // Update the idle time
                            RollingEstimate(ref _idleTime, _perfStopwatch.ElapsedMilliseconds, 10);

                            // Restart the watch to time the runtime of the action.
                            _perfStopwatch.Restart();

                            // Catch any exceptions and ignore for now.
                            action.Action();


                            // Add this performance to the estimated rolling average.
                            RollingEstimate(ref action.AverageUsageTime, _perfStopwatch.ElapsedMilliseconds, 10);

                            // Called for testing purposes.
                            if(Idle != null && Queued == 0)
                                Idle.Invoke(this);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignored
                    }
                    catch
                    {
                        if (action == null)
                            continue;

                        // If an exception was thrown on an action processing, then increment the exception counter and add the performance data.
                        RollingEstimate(ref action.AverageUsageTime, _perfStopwatch.ElapsedMilliseconds, 10);
                        Interlocked.Increment(ref action.ThrownExceptionsCount);
                    }
                }


                // Stop the timer as we have existed the main loop.
                _perfStopwatch.Stop();
            }

            /// <summary>
            /// QueueOnce a specific process action to execute.
            /// </summary>
            /// <param name="registeredAction">ThreadProcess action to execute.</param>
            /// <param name="transferred">If this item was transferred to this thread from another thread.</param>
            public void Queue(RegisteredAction registeredAction, bool transferred)
            {
                Interlocked.Increment(ref Queued);

                if (transferred == false)
                    Interlocked.Increment(ref registeredAction.QueuedCount);

                _actions.TryAdd(registeredAction);
            }

            /// <summary>
            /// Adds the specified action from the list of registered actions for this processor.
            /// </summary>
            /// <param name="action">Action to register.</param>
            public void RegisterAction(RegisteredAction action)
            {
                Interlocked.Increment(ref _registeredActionsCount);
                _registeredActions.TryAdd(action.Id, action);
                action.ProcessorThread = this;
            }

            /// <summary>
            /// Removes the specified action from the list of registered actions for this processor.
            /// </summary>
            /// <param name="action">Action to de-register.</param>
            public void DeregisterAction(RegisteredAction action)
            {
                Interlocked.Decrement(ref _registeredActionsCount);
                _registeredActions.TryRemove(action.Id, out var removedAction);
                removedAction.TransferLock.Reset();
                removedAction.ProcessorThread = null;
            }

            /// <summary>
            /// Gets the requested number of actions registered in this processor.
            /// </summary>
            /// <param name="count">Number of actions to get.  Use -1 for all actions.</param>
            /// <returns>Returns all, some or none actions.  Always returns an array.  May be an empty array.</returns>
            public RegisteredAction[] GetActions(int count)
            {
                if (count == -1)
                    return _registeredActions
                        .Select(kvp => kvp.Value)
                        .OrderByDescending(ra => ra.AverageUsageTime)
                        .ToArray();

                return _registeredActions
                    .Select(kvp => kvp.Value)
                    .OrderByDescending(ra => ra.AverageUsageTime)
                    .Take(count)
                    .ToArray();
            }


            /// <summary>
            /// Stops the loop, clears the calling queue and stops the thread from running
            /// </summary>
            public void Stop()
            {
                Pause();

                // Remove all the actions from the collection.
                while (_actions.Count > 1)
                    _actions.Take();

                foreach (var registeredAction in _registeredActions)
                {
                    // Puts a hold on all attempted queues to the processor until this action has been transferred.
                    if (registeredAction.Value.TransferLock.IsSet)
                        registeredAction.Value.TransferLock.Reset();
                }
            }

            /// <summary>
            /// Pauses the loop and stops the thread from running.
            /// </summary>
            public void Pause()
            {
                _isRunning = false;
                _cancellationTokenSource.Cancel();
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

            public override string ToString()
            {
                return
                    $"Name: {_name}; Running: {_isRunning}; Complete Time: {_idleTime}; Registered Actions: {_registeredActionsCount}, Queued: {Queued}";
            }
        }
    }
}