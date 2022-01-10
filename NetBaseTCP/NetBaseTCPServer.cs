using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;

using EMI.Network;
using EMI.ProBuffer;
using EMI.MyException;

namespace NetBaseTCP
{
    public class NetBaseTCPServer : INetworkServer
    {
        public ProArrayBuffer ProArrayBuffer { get; set; }

        private TcpListener TcpListener;
        internal List<NetBaseTCPClient> TCPClients = new List<NetBaseTCPClient>(10);

        public async Task<INetworkClient> AcceptClient()
        {
            try
            {
                var tcp = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                return new NetBaseTCPClient(this, tcp);
            }
            catch
            {
                return null;
            }
        }

        public void StartServer(string address)
        {
            if (TcpListener != null)
                throw new AlreadyException();
            TcpListener = new TcpListener(Utilities.ParseAddress(address));
            TcpListener.Start();
        }

        public void StopServer()
        {
            TcpListener.Stop();
        }
    }
}