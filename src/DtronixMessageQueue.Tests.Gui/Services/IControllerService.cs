using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DtronixMessageQueue.Rpc;

namespace DtronixMessageQueue.Tests.Gui.Services
{
    interface IControllerService : IRemoteService<ControllerSession, RpcConfig>
    {
        void ClientReady();
        void StartConnectionTest(int clients, int packageLength, int period);
        void StartMaxThroughputTest(int clientConnections, int controlConfigFrames, int controlConfigFrameSize);
        void StopTest();
        void PauseTest();
        void CloseClient();
        
    }
}
