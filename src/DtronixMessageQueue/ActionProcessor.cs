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

            AddThread(threads);

            _supervisorTimer = new Timer(Supervise);
        }

        /// <summary>
        /// Adds the specified number of threads to the processor.
        /// </summary>
        /// <param name="count">Number of threads to add.</param>
        public void AddThread(int count)
        {

            var totalRegisteredActions = _registeredActions.Count;

            var totalNewThreads = _threads.Count + count;

            var actionsPerThread = totalRegisteredActions / totalNewThreads;

            for (var i = 0; i < count; i++)
            {
                var id = Interlocked.Increment(ref _threadId);
                var pThread = new ProcessorThread($"dmq-{_name}-{id}");
                _threads.Add(pThread);

                for (int j = 0; j < actionsPerThread; j++)
                {
                    var mostActiveProcessor = GetProcessorThread(SortOrder.MostRegistered);
                    var transferAction = mostActiveProcessor.GetActions(1);

                    // If we did not get an action from this processor, repeat the loop.
                    // Usually only occurs if the thread has just recently removed the 
                    if (transferAction != null)
                        TransferAction(transferAction[0], pThread);
                    
                }
            }
        }

        /// <summary>
        /// Removes the least active thread and moves all queued actions to the next least active thread.
        /// Requires at least 2 threads to run.
        /// </summary>
        /// <param name="i">Number of threads to remove</param>
        public void RemoveThread(int i)
        {
            if (_threads.Count < 2)
                throw new InvalidOperationException($"Can not remove {i} threads.  Must maintain at least one thread for execution.  ");
            
            // Remove the least active thread from the list of active threads.
            var leastActiveProcessor = GetProcessorThread(SortOrder.LeastRegistered);
            _threads.Remove(leastActiveProcessor);

            // Get the next least active processor thread.
            var nextLeastActiveProcessor = GetProcessorThread(SortOrder.LeastRegistered);

            var registeredActions = leastActiveProcessor.GetActions(-1);
            

            // Transfer the queues from one thread to the other.
            foreach (var registeredAction in registeredActions)
            {
                TransferAction(registeredAction, nextLeastActiveProcessor);
            }

            var queuedActions = leastActiveProcessor.Stop();


            foreach (var queuedAction in queuedActions)
            {
                nextLeastActiveProcessor.Queue(queuedAction, true);
            }
            

        }

        /// <summary>
        /// Stops and transfers all actions from one thread to a specified thread.
        /// </summary>
        /// <param name="source">Source thread to stop and remove all actions from</param>
        /// <param name="destination">Thread to move all the actions to.</param>
        private void TransferAction(RegisteredAction registeredAction, ProcessorThread destination)
        {
            registeredAction.ProcessorThread.DeregisterAction(registeredAction);
            destination.RegisterAction(registeredAction);

            // Set the queue to continue to allow adds.
            registeredAction.ResetEvent.Set();
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

        internal RegisteredAction GetActionById(T id)
        {
            return _registeredActions[id];
        }

        private ProcessorThread GetProcessorThread(SortOrder order)
        {
            var processorSorter = _threads.Where(pt => IsRunning);

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
            public ManualResetEventSlim ResetEvent = new ManualResetEventSlim(true);
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
            private ConcurrentDictionary<T, RegisteredAction> _registeredActions;

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
                // Restart the stop watch to clear any existing time data.
                _perfStopwatch.Restart();

                // Loop while the cancellation of the thread has not been requested.
                while (IsRunning)
                {
                    // Update the idle time
                    RollingEstimate(ref _idleTime, _perfStopwatch.ElapsedMilliseconds, 10);

                    while (_actions.TryTake(out RegisteredAction action, 1000, _cancellationTokenSource.Token))
                    {

                        // Decrement the total actions queued and the current action being run.
                        Interlocked.Decrement(ref action.QueuedCount);
                        Interlocked.Decrement(ref Queued);

                        // If this action was transferred to another thread, queue the action up on that other thread.
                        if (action.ProcessorThread != this)
                        {
                            action.ProcessorThread.Queue(action, true);
                            continue;
                        }

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
            /// QueueOnce a specific process action to execute.
            /// </summary>
            /// <param name="registeredAction">ThreadProcess action to execute.</param>
            /// <param name="transferred">If this item was transferred to this thread from another thread.</param>
            public void Queue(RegisteredAction registeredAction, bool transferred)
            {
                Interlocked.Increment(ref Queued);

                if (transferred == false)
                {
                    Interlocked.Increment(ref registeredAction.QueuedCount);
                }
                _actions.TryAdd(registeredAction);
            }

            public void RegisterAction(RegisteredAction action)
            {
                Interlocked.Increment(ref _registeredActionsCount);
                _registeredActions.TryAdd(action.Id, action);
                action.ProcessorThread = this;
            }

            public void DeregisterAction(RegisteredAction action)
            {
                Interlocked.Decrement(ref _registeredActionsCount);
                _registeredActions.TryRemove(action.Id, out var removedAction);
                removedAction.ResetEvent.Reset();
                removedAction.ProcessorThread = null;
            }

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
            /// Stops the loop and stops the thread from running 
            /// and returns all actions registered to execute.
            /// </summary>
            /// <returns>Returns all registered actions for this processor.</returns>
            public RegisteredAction[] Stop()
            {
                Pause();
                List<RegisteredAction> actions = new List<RegisteredAction>();

                // Remove all the actions from the collection.
                while (_actions.Count > 1)
                    actions.Add(_actions.Take());

                foreach (var registeredAction in actions)
                {
                    // Puts a hold on all attempted queues to the 
                    if (registeredAction.ResetEvent.IsSet)
                        registeredAction.ResetEvent.Reset();
                }

                return actions.ToArray();
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
                    $"Name: {_name}; Running: {_isRunning}; Idle Time: {_idleTime}; Registered Actions: {_registeredActionsCount}, Queued: {Queued}";
            }
        }


    }
}
