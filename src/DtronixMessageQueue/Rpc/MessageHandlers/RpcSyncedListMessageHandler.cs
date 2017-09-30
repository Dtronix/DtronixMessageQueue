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
    public class RpcSyncedListMessageHandler<TSession, TConfig> : MessageHandler<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {
        /// <summary>
        /// Id byte which precedes all messages all messages of this type.
        /// </summary>
        public sealed override byte Id => 2;


        public RpcSyncedListMessageHandler(TSession session) : base(session)
        {
            
            
            Handlers.Add((byte)RpcSyncedListMessageAction.Create, ProcessCreateAction);
            Handlers.Add((byte)RpcSyncedListMessageAction.Delete, ProcessDeleteAction);
            Handlers.Add((byte)RpcSyncedListMessageAction.Add, ProcessAddAction);
            Handlers.Add((byte)RpcSyncedListMessageAction.Remove, ProcessRemoveAction);
            Handlers.Add((byte)RpcSyncedListMessageAction.Clear, ProcessClearAction);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the Rpc call.</param>
        private void ProcessCreateAction(byte actionhandler, MqMessage message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the Rpc call.</param>
        private void ProcessDeleteAction(byte actionhandler, MqMessage message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the Rpc call.</param>
        private void ProcessAddAction(byte actionhandler, MqMessage message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the Rpc call.</param>
        private void ProcessRemoveAction(byte actionhandler, MqMessage message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionId">byte associated with the RpcCallMessageAction enum.</param>
        /// <param name="message">Message containing the Rpc call.</param>
        private void ProcessClearAction(byte actionhandler, MqMessage message)
        {
            throw new NotImplementedException();
        }


    
    }
}