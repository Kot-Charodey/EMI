namespace EMI.DebugServer
{
    using NetworkData;
    using EMI.Indicators;

    public static class EDebuggerServerRC
    {
        public static readonly Indicator.Func<AllInfo> OnSendAllInfo = new Indicator.Func<AllInfo>("OnSendAllInfo");
        public static readonly Indicator.Func<RPCInfo[]> OnSendRPCInfo = new Indicator.Func<RPCInfo[]>("OnSendRPCInfo");

        public static readonly Indicator.FuncOut<ServerInfo> GetServerInfo = new Indicator.FuncOut<ServerInfo>("GetServerInfo");
        public static readonly Indicator.FuncOut<ClientInfo[]> GetClientInfos = new Indicator.FuncOut<ClientInfo[]>("GetClientInfos");
    }
}
