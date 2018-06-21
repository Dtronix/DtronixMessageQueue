using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DtronixMessageQueue.Rpc
{
    public class ResponseWait<T> : ConcurrentDictionary<ushort, T>
        where T : ResponseWaitHandle, new()
    {
        /// <summary>
        /// Current call Id which gets incremented for each call return request.
        /// </summary>
        private int _id;

        /// <summary>
        /// Lock to increment and loop return ID.
        /// </summary>
        private readonly object _idLock = new object();


        /// <summary>
        /// Creates a waiting operation for this session.  Could be a remote cancellation request or a pending result request.
        /// </summary>
        /// <returns>Wait operation to wait on.</returns>
        public T CreateWaitHandle(ushort? handleId)
        {
            var returnWait = new T();

            if (handleId.HasValue)
            {
                returnWait.Id = handleId.Value;
            }
            else
            {
                returnWait.ReturnResetEvent = new ManualResetEventSlim();
                // Lock the id incrementation to prevent duplicates.
                lock (_idLock)
                {
                    if (++_id > ushort.MaxValue)
                        _id = 0;

                    returnWait.Id = (ushort) _id;
                }
            }


            // Add the wait to the outstanding wait dictionary for retrieval later.
            if (TryAdd(returnWait.Id, returnWait) == false)
            {
                throw new InvalidOperationException($"Id {returnWait.Id} already exists in the handles dictionary.");
            }

            return returnWait;
        }

        /// <summary>
        /// Called to complete operation on this connection.
        /// </summary>
        /// <param name="handleId">Id of the waiting operation to complete.</param>
        public T Remove(ushort handleId)
        {
            T callWaitHandle;

            // Try to get the wait.  If the Id does not exist, the wait operation has already been completed or removed.
            if (TryRemove(handleId, out callWaitHandle))
            {
                return callWaitHandle;
            }

            return default(T);
        }
    }
}