using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Rpc.MessageHandlers;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Proxy class which will handle a method call from the specified class and execute it on a remote connection.
    /// </summary>
    /// <typeparam name="T">Type of class to proxy. method calls.</typeparam>
    /// <typeparam name="TSession">Session to proxy the method calls over.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection</typeparam>
    public class RpcProxy<T, TSession, TConfig> : DispatchProxy
        where T : IRemoteService<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        /// <summary>
        /// Name of the service on the remote server.
        /// </summary>
        public readonly string ServiceName;

        private readonly RpcCallMessageHandler<TSession, TConfig> _callMessageHandler;

        /// <summary>
        /// Session used to convey the proxied methods over.
        /// </summary>
        private readonly TSession _session;

        /// <summary>
        /// Creates instance with the specified proxied class and session.
        /// </summary>
        /// <param name="serviceName">Name of this service on the remote server.</param>
        /// <param name="session">Session to convey proxied method calls over.</param>
        /// <param name="callMessageHandler">Call handler for the RPC session.</param>
        public RpcProxy(string serviceName, RpcSession<TSession, TConfig> session,
            RpcCallMessageHandler<TSession, TConfig> callMessageHandler)
        {
            ServiceName = serviceName;
            _callMessageHandler = callMessageHandler;
            _session = (TSession) session;
        }

        /// <summary>
        /// Method invoked when a proxied method is called.
        /// </summary>
        /// <param name="msg">Information about the method called.</param>
        /// <returns>Method call result.</returns>
        protected override object Invoke(MethodInfo methodInfo, object[] arguments)
        {
            Create
            if (_session.Authenticated == false)
            {
                throw new InvalidOperationException("Session is not authenticated.  Must be authenticated before calling proxy methods.");
            }

            var serializer = _session.SerializationCache.Get();


            // Get the called method's arguments.
            ResponseWaitHandle returnWait = null;
            RpcCallMessageAction callType;

            // Determine what kind of method we are calling.
            if (methodInfo.ReturnType == typeof(void))
            {
                // Byte[0] The call has no return value so we are not waiting.
                callType = RpcCallMessageAction.MethodCallNoReturn;
            }
            else
            {
                // Byte[0] The call has a return value so we are going to need to wait on the resposne.
                callType = RpcCallMessageAction.MethodCall;

                // Create a wait operation to wait for the response.
                returnWait = _callMessageHandler.ProxyWaitOperations.CreateWaitHandle(null);

                // Byte[0,1] Wait Id which is used for returning the value and cancellation.
                serializer.MessageWriter.Write(returnWait.Id);
            }

            // Write the name of this service class.
            serializer.MessageWriter.Write(ServiceName);

            // Method name which will be remotely invoked.
            serializer.MessageWriter.Write(methodInfo.Name);

            // Total number of arguments being serialized and sent.
            serializer.MessageWriter.Write((byte) arguments.Length);

            // Serialize all arguments to the message.
            for (var i = 0; i < arguments.Length; i++)
            {
                try
                {
                    serializer.SerializeToWriter(arguments[i], i);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Argument {i} can not be converted to a protobuf object.", e);
                }
                
            }

            var message = serializer.MessageWriter.ToMessage(true);
            // Send the message over the session.
            _callMessageHandler.SendHandlerMessage((byte) callType, message);

            // If there is no return wait, our work on this session is complete.
            if (returnWait == null)
            {
                return null;
            }

            // Wait for the completion of the remote call.
            try
            {
                returnWait.ReturnResetEvent.Wait(_session.Config.RpcExecutionTimeout);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Wait handle timed out waiting for a response.");
            }

            try
            {
                // Start parsing the received message.
                serializer.MessageReader.Message = returnWait.Message;

                // Skip 2 bytes for the return ID
                serializer.MessageReader.Skip(2);

                // Reads the rest of the message for the return value.
                serializer.PrepareDeserializeReader();


                switch ((RpcCallMessageAction) returnWait.MessageActionId)
                {
                    case RpcCallMessageAction.MethodReturn:

                        // Deserialize the return value and return it to the local method call.
                        var returnValue = serializer.DeserializeFromReader(methodInfo.ReturnType, 0);
                        return returnValue;

                    case RpcCallMessageAction.MethodException:

                        // Deserialize the exception and let the local method call receive it.
                        var returnException = serializer.DeserializeFromReader(typeof(RpcRemoteExceptionDataContract), 0);
                        throw new RpcRemoteException((RpcRemoteExceptionDataContract)returnException);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                // Always return the store to the holder.
                _session.SerializationCache.Put(serializer);
            }
        }

    }
}