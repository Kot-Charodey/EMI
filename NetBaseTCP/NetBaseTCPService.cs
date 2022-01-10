using EMI.Network;

namespace NetBaseTCP
{
    public class NetBaseTCPService : INetworkService
    {
        public readonly static NetBaseTCPService Service = new NetBaseTCPService();

        public INetworkClient GetNewClient()
        {
            return new NetBaseTCPClient();
        }

        public INetworkServer GetNewServer()
        {
            return new NetBaseTCPServer();
        }
    }
}
