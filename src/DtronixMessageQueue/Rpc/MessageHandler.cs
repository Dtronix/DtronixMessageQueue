using System.Collections.Generic;
using DtronixMessageQueue.TransportLayer;

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
        /// Session of the connection.
        /// </summary>
        public TSession Session;

        /// <summary>
        /// All registered handlers for the MessageHandler.
        /// </summary>
        protected Dictionary<byte, ActionHandler> Handlers = new Dictionary<byte, ActionHandler>();

        protected MessageHandler(TSession session)
        {
            Session = session;
        }

        /// <summary>
        /// Parses and passes the message to the correct action in the handler.
        /// Automatically removes the first frame of the message used to determine which action id to call.
        /// </summary>
        /// <param name="message">Message to parse</param>
        /// <returns>True on successful parsing.</returns>
        public bool HandleMessage(MqMessage message)
        {
            if (message[0][0] != Id)
            {
                Session.Close(SessionCloseReason.ProtocolError);
            }

            // Read the type of message.
            var messageType = message[0].ReadByte(1);

            if (Handlers.ContainsKey(messageType))
            {
                message.RemoveAt(0);
                Handlers[messageType].Invoke(messageType, message);
                return true;
            }

            // Unknown message type passed.  Disconnect the connection.
            Session.Close(SessionCloseReason.ProtocolError);
            return false;
        }

        /// <summary>
        /// Send a null message to the 
        /// </summary>
        /// <param name="actionId"></param>
        public void SendHandlerMessage(byte actionId)
        {
            SendHandlerMessage(actionId, null);
        }

        /// <summary>
        /// Sends the passed message to the specified action handler.
        /// </summary>
        /// <param name="actionId">Action enum value to pass the message to.</param>
        /// <param name="message">Message to send to the connection.</param>
        public void SendHandlerMessage(byte actionId, MqMessage message)
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