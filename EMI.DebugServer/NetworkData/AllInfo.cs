namespace EMI.DebugServer.NetworkData
{
    public struct AllInfo
    {
        public ServerInfo ServerInfo;
        public ClientInfo[] ClientInfos;
        public RPCInfo[] RPCInfo;
        public NGCInfo NGCInfo;

        public AllInfo(ServerInfo serverInfo, ClientInfo[] clientInfos, RPCInfo[] rPCInfo)
        {
            NGCInfo = new NGCInfo();
            NGCInfo.Create();

            ServerInfo = serverInfo;
            ClientInfos = clientInfos;
            RPCInfo = rPCInfo;
        }
    }
}
