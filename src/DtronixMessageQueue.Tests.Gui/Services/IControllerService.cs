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
        void StartConnectionTest(int clients, int packageLength, int perioid);
        void StopTest();
        void CloseClient();
    }
}
