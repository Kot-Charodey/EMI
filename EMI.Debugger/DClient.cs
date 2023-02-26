namespace EMI.Debugger
{
    using EMI.DebugServer.NetworkData;
    using System.Collections.Generic;

    public class DClient
    {
        public ClientInfo ClientInfo;
        public RPCInfo RPC = RPCInfo.Create();
        public List<MSG> Logs = new();
    }
}
