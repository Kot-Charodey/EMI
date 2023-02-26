namespace EMI.DebugServer.NetworkData
{
    public struct AllInfoClient
    {
        public ClientInfo ClientInfos;
        public RPCInfo RPCInfo;

        public AllInfoClient(ClientInfo clientInfos, RPCInfo rPCInfo)
        {
            ClientInfos = clientInfos;
            RPCInfo = rPCInfo;
        }
    }
}
