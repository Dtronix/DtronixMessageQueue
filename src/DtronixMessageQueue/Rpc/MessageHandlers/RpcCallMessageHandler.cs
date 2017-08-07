using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.DataContract;

namespace DtronixMessageQueue.Rpc.MessageHandlers
{
    public class RpcCallMessageHandler<TSession, TConfig> : MessageHandler<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        /// <summary>
        /// Id byte which precedes all messages all messages of this type.
        /// </summary>
        public sealed override byte Id => 1;

        /// <summary>
        /// Contains all services that can be remotely executed on this session.
        /// </summary>
        private readonly Dictionary<string, IRemoteService<TSession, TConfig>> _serviceInstances =
            new Dictionary<string, IRemoteService<TSession, TConfig>>();

        

        /// <summary>
        /// Proxy objects to be invoked on this session and proxied to the recipient session.
        /// </summary>
        public readonly Dictionary<Type, IRemoteService<TSession, TConfig>> RemoteServicesProxy =
            new Dictionary<Type, IRemoteService<TSession, TConfig>>();

        /// <summary>
        ///  Base proxy to the used for servicing of the proxy interface.
        /// </summary>
        public readonly Dictionary<Type, RealProxy> RemoteServiceRealproxy = new Dictionary<Type, RealProxy>();

        public readonly ResponseWait<ResponseWaitHandle> ProxyWaitOperations = new ResponseWait<ResponseWaitHandle>();

        private readonly ResponseWait<ResponseWaitHandle> _remoteWaitOperations = new ResponseWait<ResponseWaitHandle>();

        public RpcCallMessageHandler(TSession session) : base(session)
        {
            Handlers.Add((byte) RpcCallMessageAction.MethodCancel, MethodCancelAction);

            Handlers.Add((byte) RpcCallMessageAction.MethodCallNoReturn, ProcessRpcCallAction);
            Handlers.Add((byte) RpcCallMessageAction.MethodCall, ProcessRpcCallAction);

            Handlers.Add((byte) RpcCallMessageAction.MethodException, ProcessRpcReturnAction);
            Handlers.Add((byte) RpcCallMessageAction.MethodReturn, ProcessRpcReturnAction);
        }

        public void AddService<T>(T instance) where T : IRemoteService<TSession, TConfig>
        {
            // Add the instance to this handler.
            _serviceInstances.Add(instance.Name, instance);
        }

        /// <summary>
        /// Cancels the specified action.
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.></param>
        /// <param name="message">Message containing the cancellation information.</param>
        public void MethodCancelAction(byte actionId, MqMessage message)
        {
            var cancellationId = message[0].ReadUInt16(0);
            _remoteWaitOperations.Cancel(cancellationId);
        }


        /// <summary>
        /// Processes the incoming Rpc call from the recipient connection.
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the Rpc call.</param>
        private void ProcessRpcCallAction(byte actionId, MqMessage message)
        {
            // Execute the processing on the worker thread.
            Task.Run(() =>
            {
                var messageType = (RpcCallMessageAction)actionId;

                // Retrieve a serialization cache to work with.
                var serialization = Session.SerializationCache.Get(message);
                ushort recMessageReturnId = 0;

                try
                {
                    // Determine if this call has a return value.
                    if (messageType == RpcCallMessageAction.MethodCall)
                    {
                        recMessageReturnId = serialization.MessageReader.ReadUInt16();
                    }

                    // Read the string service name, method and number of arguments.
                    var recServiceName = serialization.MessageReader.ReadString();
                    var recMethodName = serialization.MessageReader.ReadString();
                    var recArgumentCount = serialization.MessageReader.ReadByte();

                    // Get the method info from the cache.
                    var serviceMethod = Session.ServiceMethodCache.GetMethodInfo(recServiceName, recMethodName);
                   

                    // Verify that the requested service exists.
                    if (serviceMethod == null || !_serviceInstances.ContainsKey(recServiceName))
                        throw new Exception($"Service '{recServiceName}' does not exist.");

                    ResponseWaitHandle cancellationWait = null;

                    // If the past parameter is a cancellation token, setup a return wait for this call to allow for remote cancellation.
                    if (recMessageReturnId != 0 && serviceMethod.HasCancellation)
                    {
                        cancellationWait = _remoteWaitOperations.CreateWaitHandle(recMessageReturnId);

                        cancellationWait.TokenSource = new CancellationTokenSource();
                        cancellationWait.Token = cancellationWait.TokenSource.Token;
                    }

                    // Setup the parameters to pass to the invoked method.
                    object[] parameters = new object[recArgumentCount + (cancellationWait == null ? 0 : 1)];

                    // Determine if we have any parameters to pass to the invoked method.
                    if (recArgumentCount > 0)
                    {
                        serialization.PrepareDeserializeReader();

                        // Parse each parameter to the parameter list.
                        for (var i = 0; i < recArgumentCount; i++)
                        {
                            if (serviceMethod.ParameterTypes[i] == typeof(RpcStream<TSession, TConfig>))
                            {
                                var streamId = (ushort)serialization.DeserializeFromReader(typeof(ushort), i);
                                // If this is a stream, setup the receiving end.
                                parameters[i] = new RpcStream<TSession, TConfig>(Session, streamId);

                            }
                            else
                            {
                                parameters[i] = serialization.DeserializeFromReader(serviceMethod.ParameterTypes[i], i);
                            }
                        }
                        
                    }

                    // Add the cancellation token to the parameters.
                    if (cancellationWait != null)
                        parameters[parameters.Length - 1] = cancellationWait.Token;


                    object returnValue;
                    try
                    {
                        // Invoke the requested method.
                        returnValue = serviceMethod.Invoke(_serviceInstances[recServiceName], parameters);
                    }
                    catch (Exception ex)
                    {
                        // Determine if this method was waited on.  If it was and an exception was thrown,
                        // Let the recipient session know an exception was thrown.
                        if (recMessageReturnId != 0 &&
                            ex.InnerException?.GetType() != typeof(OperationCanceledException))
                        {
                            SendRpcException(serialization, ex, recMessageReturnId);
                        }
                        return;
                    }
                    finally
                    {
                        _remoteWaitOperations.Remove(recMessageReturnId);
                    }


                    // Determine what to do with the return value.
                    if (messageType == RpcCallMessageAction.MethodCall)
                    {
                        // Reset the stream.
                        serialization.Stream.SetLength(0);
                        serialization.MessageWriter.Clear();

                        // Write the return id.
                        serialization.MessageWriter.Write(recMessageReturnId);

                        // Serialize the return value and add it to the stream.
                        serialization.SerializeToWriter(returnValue, 0);

                        // Send the return value message to the recipient.
                        SendHandlerMessage((byte)RpcCallMessageAction.MethodReturn,
                            serialization.MessageWriter.ToMessage(true));
                    }
                }
                catch (Exception ex)
                {
                    // If an exception occurred, notify the recipient connection.
                    SendRpcException(serialization, ex, recMessageReturnId);
                }
                finally
                {
                    // Return the serialization to the cache to be reused.
                    Session.SerializationCache.Put(serialization);
                }
            });
        }


        /// <summary>
        /// Processes the incoming return value message from the recipient connection.
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the frames for the return value.</param>
        private void ProcessRpcReturnAction(byte actionId, MqMessage message)
        {
            // Execute the processing on the worker thread.
            Task.Run(() =>
            {
                // Read the return Id.
                var returnId = message[0].ReadUInt16(0);


                ResponseWaitHandle callWaitHandle = ProxyWaitOperations.Remove(returnId);
                // Try to get the outstanding wait from the return id.  If it does not exist, the has already completed.
                if (callWaitHandle != null)
                {
                    callWaitHandle.Message = message;

                    callWaitHandle.MessageActionId = actionId;

                    // Release the wait event.
                    callWaitHandle.ReturnResetEvent.Set();
                }
            });
        }

        /// <summary>
        /// Takes an exception and serializes the important information and sends it to the recipient connection.
        /// </summary>
        /// <param name="serialization">Serialization information to use for this response.</param>
        /// <param name="ex">Exception which occurred to send to the recipient session.</param>
        /// <param name="messageReturnId">Id used to reference this call on the recipient session.</param>
        private void SendRpcException(SerializationCache.Serializer serialization, Exception ex, ushort messageReturnId)
        {
            // Reset the length of the stream to clear it.
            serialization.Stream.SetLength(0);

            // Clear the message writer of any previously stored data.
            serialization.MessageWriter.Clear();

            // Writer the Rpc call type and the return Id.
            serialization.MessageWriter.Write(messageReturnId);

            // Get the exception information in a format that we can serialize.
            var exception = new RpcRemoteExceptionDataContract(ex is TargetInvocationException ? ex.InnerException : ex);

            // Serialize the class with a length prefix.
            serialization.SerializeToWriter(exception, 0);

            // Send the message to the recipient connection.
            SendHandlerMessage((byte) RpcCallMessageAction.MethodException, serialization.MessageWriter.ToMessage(true));
        }
    }
}