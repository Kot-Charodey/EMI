using EMI.Network;

namespace EMI.NetParallelTCP
{
    public class NetParallelTCPService : INetworkService
    {
        public readonly static NetParallelTCPService Service = new NetParallelTCPService();

        public INetworkClient GetNewClient()
        {
            return new NetParallelTCPClient();
        }

        public INetworkServer GetNewServer()
        {
            return new NetParallelTCPServer();
        }
    }
}
