using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using EMI.Network;
using System.Threading;
using EMI.NGC;
using EMI.MyException;

namespace EMI.NetParallelTCP
{
    public class NetParallelTCPServer : INetworkServer
    {

        private TcpListener TcpListener;
        internal List<NetParallelTCPClient> TCPClients = new List<NetParallelTCPClient>(10);

        public async Task<INetworkClient> AcceptClient(CancellationToken token)
        {
            try
            {
                var tcp = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                return new NetParallelTCPClient(this, tcp);
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