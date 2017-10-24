using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Rpc.MessageHandlers;
using DtronixMessageQueue.TransportLayer;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Session to handle all Rpc call reading/writing for a socket session.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public abstract class RpcSession<TSession, TConfig> : MqSession<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {

        /// <summary>
        /// Cache for commonly called methods used throughout the session.
        /// </summary>
        public ServiceMethodCache ServiceMethodCache;

        public Dictionary<byte, MessageHandler<TSession, TConfig>> MessageHandlers { get; }

        protected RpcCallMessageHandler<TSession, TConfig> RpcCallHandler;

        /// <summary>
        /// Store which contains instances of all classes for serialization and destabilization of data.
        /// </summary>
        public SerializationCache SerializationCache { get; set; }

        /// <summary>
        /// Server base socket for this session.
        /// Null if the MqSessionHandler is not running in server mode.
        /// </summary>
        public RpcServer<TSession, TConfig> Server { get; private set; }

        /// <summary>
        /// Client base socket for this session.
        /// Null if the MqSessionHandler is not running in client mode.
        /// </summary>
        public RpcClient<TSession, TConfig> Client { get; private set; }

        /// <summary>
        /// Called to send authentication data to the server.
        /// </summary>
        public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> Authenticate;

        /// <summary>
        /// Event invoked once when the RpcSession has been authenticated and is ready for usage.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;

        /// <summary>
        /// True if this session has passed authentication;  False otherwise.
        /// </summary>
        public bool Authenticated { get; private set; }

        private readonly CancellationTokenSource _authTimeoutCancel = new CancellationTokenSource();

        protected RpcSession()
        {
            MessageHandlers = new Dictionary<byte, MessageHandler<TSession, TConfig>>();
        }


        /// <summary>
        /// Called when this session is being setup.
        /// </summary>
        protected override void OnSetup()
        {
            base.OnSetup();

            // Determine if this session is running on the server or client to retrieve the worker thread pool.
            if (SessionHandler.LayerMode == TransportLayerMode.Server)
                Server = (RpcServer<TSession, TConfig>) SessionHandler;
            else
                Client = (RpcClient<TSession, TConfig>) SessionHandler;

            SerializationCache = new SerializationCache(Config);

            RpcCallHandler = new RpcCallMessageHandler<TSession, TConfig>((TSession) this);
            MessageHandlers.Add(RpcCallHandler.Id, RpcCallHandler);



            // If this is a new session on the server, send the welcome message.
            if (SessionHandler.LayerMode == TransportLayerMode.Server)
            {
                Server.ServerInfo.RequireAuthentication = Config.RequireAuthentication;

                var serializer = SerializationCache.Get();

                serializer.MessageWriter.Write((byte)MqCommandType.RpcCommand);
                serializer.MessageWriter.Write((byte)RpcCommandType.WelcomeMessage);
                serializer.SerializeToWriter(Server.ServerInfo);

                var message = serializer.MessageWriter.ToMessage(true);

                message[0].FrameType = MqFrameType.Command;

                // RpcCommand:byte; RpcCommandType:byte; RpcServerInfoDataContract:byte[];
                Send(message);
            }

            // If the server does not require authentication, alert the server session that it is ready.
            if (SessionHandler.LayerMode == TransportLayerMode.Server && Config.RequireAuthentication == false)
            {
                Authenticated = true;
                Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession)this));
            }

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Config.ConnectionTimeout, _authTimeoutCancel.Token);
                }
                catch
                {
                    return;
                }

                if (!_authTimeoutCancel.IsCancellationRequested)
                    Close(SessionCloseReason.AuthenticationFailure);
            });

        }


        /// <summary>
        /// Processes an incoming command frame from the connection.
        /// Captures the call if it is a Rpc command.
        /// </summary>
        /// <param name="frame">Command frame to process.</param>
        protected override void ProcessCommand(MqFrame frame)
        {
            var commandType = (MqCommandType) frame.ReadByte(0);
            

            // If this is a base MqCommand, pass this directly on to the base command handler.
            if (commandType != MqCommandType.RpcCommand)
            {
                Utilities.TraceHelper($"{SessionHandler.LayerMode} {commandType} Non RPC Command Processing");
                base.ProcessCommand(frame);
                return;
            }

            try
            {
                var rpcCommandType = (RpcCommandType) frame.ReadByte(1);

                if (rpcCommandType == RpcCommandType.WelcomeMessage)
                {
                    Utilities.TraceHelper($"{SessionHandler.LayerMode} {rpcCommandType} Command Processing");
                    // RpcCommand:byte; RpcCommandType:byte; RpcServerInfoDataContract:byte[];

                    // Ensure that this command is running on the client.
                    if (SessionHandler.LayerMode != TransportLayerMode.Client)
                    {
                        Close(SessionCloseReason.ProtocolError);
                        return;
                    }

                    var serializer = SerializationCache.Get(new MqMessage(frame));

                    // Forward the reader two bytes to the data.
                    serializer.MessageReader.ReadBytes(2);

                    serializer.PrepareDeserializeReader();

                    // Try to read the information from the server about the server.
                    Client.ServerInfo =
                        serializer.DeserializeFromReader(typeof(RpcServerInfoDataContract)) as RpcServerInfoDataContract;

                    if (Client.ServerInfo == null)
                    {
                        Close(SessionCloseReason.ProtocolError);
                        return;
                    }

                    // Check to see if the server requires authentication.  If so, send a auth check.
                    if (Client.ServerInfo.RequireAuthentication)
                    {

                        var authArgs = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession)this);

                        // Start the authentication event to get the auth data.
                        Authenticate?.Invoke(this, authArgs);

                        // Send the auth frame
                        Send(new MqFrame(authArgs.AuthData, MqFrameType.Last, Config));
                    }
                    else
                    {
                        // If no authentication is required, set this client to authenticated.
                        Authenticated = true;

                        // Alert the server that this session is ready for usage.
                        Task.Run(
                            () => { Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this)); });
                    }

                    SerializationCache.Put(serializer);
                }
                else if (rpcCommandType == RpcCommandType.AuthenticationSuccess)
                {
                    Utilities.TraceHelper($"{SessionHandler.LayerMode} {rpcCommandType} Command Processing");
                    // RpcCommand:byte; RpcCommandType:byte; AuthResult:bool;

                    // Cancel the timeout request.
                    _authTimeoutCancel.Cancel();

                    // Ensure that this command is running on the client.
                    if (SessionHandler.LayerMode != TransportLayerMode.Client)
                    {
                        Close(SessionCloseReason.ProtocolError);
                        return;
                    }

                    if (Client.Config.RequireAuthentication == false)
                    {
                        Close(SessionCloseReason.ProtocolError);
                        return;
                    }

                    Authenticated = true;

                    // Alert the client that this session is ready for usage.
                    Task.Run(() => { Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this)); });
                }
                else
                {
                    Close(SessionCloseReason.ProtocolError);
                }
            }
            catch (Exception)
            {
                Close(SessionCloseReason.ProtocolError);
            }
        }

        private bool AuthenticateSession(MqMessage message)
        {
            if (Server == null)
            {
                Close(SessionCloseReason.ProtocolError);
                return false;
            }

            var authArgs = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this)
            {
                AuthData = message[0].Buffer
            };
            Authenticate?.Invoke(this, authArgs);

            if (!authArgs.Authenticated)
            {
                Close(SessionCloseReason.AuthenticationFailure);
                return false;
            }

            var authFrame = CreateFrame(new byte[authArgs.AuthData.Length + 2], MqFrameType.Command);
            authFrame.Write(0, (byte) MqCommandType.RpcCommand);
            authFrame.Write(1, (byte) RpcCommandType.AuthenticationSuccess);

            // State of the authentication
            authFrame.Write(2, Authenticated);

            // RpcCommand:byte; RpcCommandType:byte; AuthResult:bool;
            Send(authFrame);

            // Alert the server that this session is ready for usage.
            Task.Run(
                () => { Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this)); });

            Authenticated = true;

            _authTimeoutCancel.Cancel();

            return true;

        }

        /// <summary>
        /// Event fired when one or more new messages are ready for use.
        /// </summary>
        /// <param name="sender">Originator of call for this event.</param>
        /// <param name="e">Event args for the message.</param>
        protected override void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e)
        {
            MqMessage message;

            // Continue to parse the messages in this queue.
            while (e.Messages.Count > 0)
            {
                message = e.Messages.Dequeue();

                if (!Authenticated && Server != null)
                {
                    if (!AuthenticateSession(message))
                        return;
                }

                // Read the first byte for the ID.
                var handlerId = message[0].ReadByte(0);
                var handledMessage = false;

                // See if we have a handler for the requested Id.
                if (MessageHandlers.ContainsKey(handlerId))
                {
                    // If anything in the message handler throws, disconnect the connection.
                    try
                    {
                        handledMessage = MessageHandlers[handlerId].HandleMessage(message);
                    }
                    catch
                    {
                        Close(SessionCloseReason.ApplicationError);
                        return;
                    }
                    
                }

                // If the we can not handle this message, disconnect the session.
                if (handledMessage == false)
                {
                    Close(SessionCloseReason.ProtocolError);
                    return;
                }
            }
        }


        /// <summary>
        /// Adds a proxy interface and instance to the current session to allow for remote method proxying.
        /// </summary>
        /// <typeparam name="T">Interface of the instance.  Must be explicitly specified.</typeparam>
        /// <param name="serviceName">Name of the service for this interface on the remote connection.</param>
        public void AddProxy<T>(string serviceName) where T : IRemoteService<TSession, TConfig>
        {
            var proxy = new RpcProxy<T, TSession, TConfig>(serviceName, this, RpcCallHandler);

            RpcCallHandler.RemoteServiceRealproxy.Add(typeof(T), proxy);
            RpcCallHandler.RemoteServicesProxy.Add(typeof(T), (T) proxy.GetTransparentProxy());
        }

        /// <summary>
        /// Returns the proxy of the specified type if it exists on this session.
        /// </summary>
        /// <typeparam name="T">Interface of the proxy to retrieve.</typeparam>
        /// <returns>Proxied interface methods.</returns>
        public T GetProxy<T>() where T : IRemoteService<TSession, TConfig>
        {
            return (T) RpcCallHandler.RemoteServicesProxy[typeof(T)];
        }

        /// <summary>
        /// Adds a service to this session to be called remotely.
        /// </summary>
        /// <typeparam name="T">Interface of this type.</typeparam>
        /// <param name="instance">Instance to execute methods on.</param>
        public void AddService<T>(T instance) where T : IRemoteService<TSession, TConfig>
        {
            instance.Session = (TSession)this;
            ServiceMethodCache.AddService(instance.Name, instance);
            RpcCallHandler.AddService(instance);
        }
    }
}