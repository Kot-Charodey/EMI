namespace EMI.DebugServer
{
    using NetworkData;
    using EMI.Indicators;

    public class EDebuggerClientRC
    {
        public static readonly Indicator.Func<AllInfoClient> OnSendAllInfo = new Indicator.Func<AllInfoClient>("OnSendAllInfo_Client");
        public static readonly Indicator.Func<RPCInfo> OnSendRPCInfo = new Indicator.Func<RPCInfo>("OnSendRPCInfo_Client");
        public static readonly Indicator.FuncOut<ClientInfo> GetClientInfo = new Indicator.FuncOut<ClientInfo>("GetClientInfo");
    }
}
