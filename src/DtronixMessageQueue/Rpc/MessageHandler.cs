using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DtronixMessageQueue.TcpSocket;

namespace DtronixMessageQueue.Rpc
{
    /// <summary>
    /// Base class to be used for all Rpc message handlers.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public abstract class MessageHandler<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        protected readonly TConfig Config;
        protected readonly SerializationCache SerializationCache;
        protected readonly ServiceMethodCache ServiceMethodCache;

        [ThreadStatic]
        public static TSession Session;

        /// <summary>
        /// Delegate used to be called on each successfully parsed action call in this MessageHandler.
        /// </summary>
        /// <param name="actionId">Value of the enum for the action called.</param>
        /// <param name="message">Full message excluding the action id.</param>
        public delegate void ActionHandler(byte actionId, MqMessage message);

        /// <summary>
        /// Id byte which precedes all messages all messages of this type.
        /// </summary>
        public abstract byte Id { get; }

        /// <summary>
        /// All registered handlers for the MessageHandler.
        /// </summary>
        protected Dictionary<byte, ActionHandler> Handlers = new Dictionary<byte, ActionHandler>();

        protected MessageHandler(TConfig config, SerializationCache serializationCache, ServiceMethodCache serviceMethodCache)
        {
            Config = config;
            SerializationCache = serializationCache;
            ServiceMethodCache = serviceMethodCache;
        }

        public bool ProcessMessage(TSession session, MqMessage message)
        {
            Session = session;
            // Read the type of message.
            var messageType = message[0].ReadByte(1);

            if (Handlers.TryGetValue(messageType, out var handler))
            {
                message.RemoveAt(0);
                handler.Invoke(messageType, message);
                return true;
            }

            // Unknown message type passed.  Disconnect the connection.
            session.Close(CloseReason.ProtocolError);
            return false;
        }

        /// <summary>
        /// Send a null message to the 
        /// </summary>
        /// <param name="actionId"></param>
        protected void SendHandlerMessage(byte actionId)
        {
            SendHandlerMessage(actionId, null);
        }

        /// <summary>
        /// Sends the passed message to the specified action handler.
        /// </summary>
        /// <param name="actionId">Action enum value to pass the message to.</param>
        /// <param name="message">Message to send to the connection.</param>
        protected void SendHandlerMessage(byte actionId, MqMessage message)
        {
            var headerFrame = Session.CreateFrame(new byte[2], MqFrameType.More);
            // Id of the message handler.
            headerFrame.Write(0, Id);
            // Id of the action to call.
            headerFrame.Write(1, actionId);

            var sendMessage = new MqMessage
            {
                headerFrame,
                message
            };

            Session.Send(sendMessage);
        }
    }
}