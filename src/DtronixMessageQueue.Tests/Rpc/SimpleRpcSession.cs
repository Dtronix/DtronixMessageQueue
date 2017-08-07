using DtronixMessageQueue.Rpc;
using DtronixMessageQueue.Rpc.MessageHandlers;

namespace DtronixMessageQueue.Tests.Rpc
{
    public class SimpleRpcSession : RpcSession<SimpleRpcSession, RpcConfig>
    {
        public ByteTransportMessageHandler<SimpleRpcSession, RpcConfig> ByteTransport => this.ByteTransportHandler;

        public bool CreatedReceiveByteTransport => !ByteTransport.receive_operation.IsEmpty;

        public bool CreatedSendByteTransport => !ByteTransport.send_operation.IsEmpty;

    }
}