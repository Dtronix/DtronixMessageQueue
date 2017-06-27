using System;
using DtronixMessageQueue.Rpc.DataContract;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Class thrown when a remote exception occurs at a proxied method call.
    /// </summary>
    public class RpcRemoteException : Exception
    {
        /// <summary>
        /// Creates instance of the remote exception class.
        /// </summary>
        /// <param name="exception">Exception details which are used to pass along to the base exception.</param>
        public RpcRemoteException(RpcRemoteExceptionDataContract exception) : base(exception.Message)
        {
        }
    }
}