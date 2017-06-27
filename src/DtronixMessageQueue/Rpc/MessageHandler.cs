using System.Collections.Generic;
using DtronixMessageQueue.Socket;

namespace DtronixMessageQueue.Rpc
{
    public abstract class MessageHandler<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        public delegate void ActionHandler(byte actionHandler, MqMessage message);

        /// <summary>
        /// Id byte which precedes all messages all messages of this type.
        /// </summary>
        public abstract byte Id { get; }

        public TSession Session;

        protected Dictionary<byte, ActionHandler> Handlers = new Dictionary<byte, ActionHandler>();

        protected MessageHandler(TSession session)
        {
            Session = session;
        }

        public bool HandleMessage(MqMessage message)
        {
            if (message[0][0] != Id)
            {
                Session.Close(SocketCloseReason.ProtocolError);
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
            Session.Close(SocketCloseReason.ProtocolError);
            return false;
        }

        public void SendHandlerMessage(byte actionId)
        {
            SendHandlerMessage(actionId, null);
        }

        public void SendHandlerMessage(byte actionId, MqMessage message)
        {
            var headerFrame = Session.CreateFrame(new byte[2], MqFrameType.More);
            headerFrame.Write(0, Id);
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