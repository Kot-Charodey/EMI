using System;
using System.Collections.Generic;


namespace EMI.Debugger
{
    using EMI.DebugServer;
    using EMI.DebugServer.NetworkData;

    internal class RecordedData
    {
        public DebuggerType Type;
        public List<NGCInfo> NGC = new(10000);

        public Dictionary<Guid, DClient> ClientsInfo = new();
        public RPCInfo GlobalRCPInfo = new();
        public ServerInfo ServerInfo = new();
    }
}
