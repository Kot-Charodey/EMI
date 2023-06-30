using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using EMI.MyException;

namespace EMI.Network.NetTCPV3
{
    public class NetTCPV3Server : INetworkServer
    {

        private TcpListener TcpListener;
        internal List<NetTCPV3Client> TCPClients = new List<NetTCPV3Client>(10);

        public async Task<INetworkClient> AcceptClient(CancellationToken token)
        {
            try
            {
                var tcp = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                return new NetTCPV3Client(this, tcp);
            }
            catch
            {
                return null;
            }
        }

        public void StartServer(string address)
        {
            lock (this)
            {
                if (TcpListener != null)
                    throw new AlreadyException();
                TcpListener = new TcpListener(Utilities.ParseIPAddress(address));
                TcpListener.Start();
            }
        }

        public void StopServer()
        {
            lock (this)
            {
                TcpListener.Stop();
                TcpListener = null;
            }
        }
    }
}