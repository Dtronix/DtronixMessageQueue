﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc.DataContract;
using DtronixMessageQueue.Rpc.MessageHandlers;
using DtronixMessageQueue.TcpSocket;

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
        public Dictionary<byte, MessageHandler<TSession, TConfig>> MessageHandlers { get; }

        protected RpcCallMessageHandler<TSession, TConfig> RpcCallHandler;

        /// <summary>
        /// Store which contains instances of all classes for serialization and destabilization of data.
        /// </summary>
        public SerializationCache SerializationCache { get; set; }

        /// <summary>
        /// Server base socket for this session.
        /// Null if the BaseSocket is not running in server mode.
        /// </summary>
        public RpcServer<TSession, TConfig> Server { get; private set; }

        /// <summary>
        /// Client base socket for this session.
        /// Null if the BaseSocket is not running in client mode.
        /// </summary>
        public RpcClient<TSession, TConfig> Client { get; private set; }

        private ActionProcessor<Guid> _rpcActionProcessor;

        /// <summary>
        /// Verify the authenticity of the newly connected client.
        /// </summary>
        public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> Authenticate;

        /// <summary>
        /// Called when the authentication process succeeds.
        /// </summary>
        public event EventHandler<RpcAuthenticateEventArgs<TSession, TConfig>> AuthenticationSuccess;

        /// <summary>
        /// Event invoked once when the RpcSession has been authenticated and is ready for usage.
        /// </summary>
        public event EventHandler<SessionEventArgs<TSession, TConfig>> Ready;

        /// <summary>
        /// True if this session has passed authentication;  False otherwise.
        /// </summary>
        public bool Authenticated { get; private set; }

        private Task _authTimeout;

        private readonly CancellationTokenSource _authTimeoutCancel = new CancellationTokenSource();
        private ConcurrentQueue<MqMessage> ProcessQueue = new ConcurrentQueue<MqMessage>();

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
            if (SocketHandler.Mode == TcpSocketMode.Server)
            {
                Server = (RpcServer<TSession, TConfig>) SocketHandler;
                _rpcActionProcessor = Server.RpcActionProcessor;
            }
            else
            {
                Client = (RpcClient<TSession, TConfig>) SocketHandler;
                _rpcActionProcessor = Client.RpcActionProcessor;
            }

            _rpcActionProcessor.Register(Id, ProcessRpc);

            SerializationCache = new SerializationCache(Config);

            RpcCallHandler = new RpcCallMessageHandler<TSession, TConfig>((TSession) this);
            MessageHandlers.Add(RpcCallHandler.Id, RpcCallHandler);
        }

        protected override void OnDisconnected(CloseReason reason)
        {
            _rpcActionProcessor.Deregister(Id);

            base.OnDisconnected(reason);
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
                base.ProcessCommand(frame);
                return;
            }

            try
            {
                var rpcCommandType = (RpcCommandType) frame.ReadByte(1);

                if (rpcCommandType == RpcCommandType.WelcomeMessage)
                {
                    // RpcCommand:byte; RpcCommandType:byte; RpcServerInfoDataContract:byte[];

                    // Ensure that this command is running on the client.
                    if (SocketHandler.Mode != TcpSocketMode.Client)
                    {
                        Close(CloseReason.ProtocolError);
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
                        Close(CloseReason.ProtocolError);
                        return;
                    }

                    // Check to see if the server requires authentication.  If so, send a auth check.
                    if (Client.ServerInfo.RequireAuthentication)
                    {
                        var authArgs = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this);

                        // Start the authentication event to get the auth data.
                        Authenticate?.Invoke(this, authArgs);

                        serializer.MessageWriter.Write((byte) MqCommandType.RpcCommand);
                        serializer.MessageWriter.Write((byte) RpcCommandType.AuthenticationRequest);

                        if (authArgs.AuthData == null)
                        {
                            authArgs.AuthData = new byte[] {0};
                        }


                        serializer.MessageWriter.Write(authArgs.AuthData, 0, authArgs.AuthData.Length);

                        var authMessage = serializer.MessageWriter.ToMessage(true);
                        authMessage[0].FrameType = MqFrameType.Command;

                        _authTimeout = new Task(async () =>
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
                                Close(CloseReason.TimeOut);
                        });

                        // RpcCommand:byte; RpcCommandType:byte; AuthData:byte[];
                        Send(authMessage);

                        _authTimeout.Start();
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
                else if (rpcCommandType == RpcCommandType.AuthenticationRequest)
                {
                    // RpcCommand:byte; RpcCommandType:byte; AuthData:byte[];

                    // If this is not run on the server, quit.
                    if (SocketHandler.Mode != TcpSocketMode.Server)
                    {
                        Close(CloseReason.ProtocolError);
                        return;
                    }

                    // Ensure that the server requires authentication.
                    if (Server.Config.RequireAuthentication == false)
                    {
                        Close(CloseReason.ProtocolError);
                        return;
                    }

                    byte[] authBytes = new byte[frame.DataLength - 2];
                    frame.Read(2, authBytes, 0, authBytes.Length);

                    var authArgs = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this)
                    {
                        AuthData = authBytes
                    };


                    Authenticate?.Invoke(this, authArgs);

                    Authenticated = authArgs.Authenticated;

                    if (Authenticated == false)
                    {
                        Close(CloseReason.AuthenticationFailure);
                    }
                    else
                    {
                        var authFrame = CreateFrame(new byte[authArgs.AuthData.Length + 2], MqFrameType.Command);
                        authFrame.Write(0, (byte) MqCommandType.RpcCommand);
                        authFrame.Write(1, (byte) RpcCommandType.AuthenticationSuccess);

                        // RpcCommand:byte; RpcCommandType:byte;
                        Send(authFrame);

                        // Alert the server that this session is ready for usage.
                        Task.Run(
                            () => { Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this)); });
                    }
                }
                else if (rpcCommandType == RpcCommandType.AuthenticationSuccess)
                {
                    // RpcCommand:byte; RpcCommandType:byte;

                    // Cancel the timeout request.
                    _authTimeoutCancel.Cancel();

                    // Ensure that this command is running on the client.
                    if (SocketHandler.Mode != TcpSocketMode.Client)
                    {
                        Close(CloseReason.ProtocolError);
                        return;
                    }

                    if (Client.ServerInfo.RequireAuthentication == false)
                    {
                        Close(CloseReason.ProtocolError);
                        return;
                    }

                    Authenticated = true;

                    var authArgs = new RpcAuthenticateEventArgs<TSession, TConfig>((TSession) this);

                    // Alert the client that the sesion has been authenticated.
                    AuthenticationSuccess?.Invoke(this, authArgs);

                    // Alert the client that this session is ready for usage.
                    Task.Run(() => { Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this)); });
                }
                else
                {
                    Close(CloseReason.ProtocolError);
                }
            }
            catch (Exception)
            {
                Close(CloseReason.ProtocolError);
            }
        }

        /// <summary>
        /// Called when this RpcSession is connected to the socket.
        /// </summary>
        protected override void OnConnected()
        {
            // If this is a new session on the server, send the welcome message.
            if (SocketHandler.Mode == TcpSocketMode.Server)
            {
                Server.ServerInfo.RequireAuthentication = Config.RequireAuthentication;

                var serializer = SerializationCache.Get();

                serializer.MessageWriter.Write((byte) MqCommandType.RpcCommand);
                serializer.MessageWriter.Write((byte) RpcCommandType.WelcomeMessage);
                serializer.SerializeToWriter(Server.ServerInfo);

                var message = serializer.MessageWriter.ToMessage(true);

                message[0].FrameType = MqFrameType.Command;

                // RpcCommand:byte; RpcCommandType:byte; RpcServerInfoDataContract:byte[];
                Send(message);
            }

            base.OnConnected();

            // If the server does not require authentication, alert the server session that it is ready.
            if (SocketHandler.Mode == TcpSocketMode.Server && Config.RequireAuthentication == false)
            {
                Authenticated = true;
                Ready?.Invoke(this, new SessionEventArgs<TSession, TConfig>((TSession) this));
            }
        }


        /// <summary>
        /// Event fired when one or more new messages are ready for use.
        /// </summary>
        /// <param name="sender">Originator of call for this event.</param>
        /// <param name="e">Event args for the message.</param>
        protected override void OnIncomingMessage(object sender, IncomingMessageEventArgs<TSession, TConfig> e)
        {
            var totalMessages = e.Messages.Count;
            for (int i = 0; i < totalMessages; i++)
                ProcessQueue.Enqueue(e.Messages.Dequeue());

            _rpcActionProcessor.QueueOnce(Id);
        }


        private void ProcessRpc()
        {
            // Continue to parse the messages in this queue.
            while (ProcessQueue.TryDequeue(out var message))
            {
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
                        Close(CloseReason.ApplicationError);
                        return;
                    }

                }

                // If the we can not handle this message, disconnect the session.
                if (handledMessage == false)
                {
                    Close(CloseReason.ProtocolError);
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