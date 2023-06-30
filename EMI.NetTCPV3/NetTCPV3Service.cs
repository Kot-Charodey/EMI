using EMI.Network;

namespace EMI.Network.NetTCPV3
{
    public class NetTCPV3Service : INetworkService
    {
        public readonly static NetTCPV3Service Service = new NetTCPV3Service();

        public INetworkClient GetNewClient()
        {
            return new NetTCPV3Client();
        }

        public INetworkServer GetNewServer()
        {
            return new NetTCPV3Server();
        }
    }
}
