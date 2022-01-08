using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EMI.Network;
using EMI.ProBuffer;

namespace MSTests
{
    internal class NetClientTest : INetworkClient
    {
        public ProArrayBuffer ProArrayBuffer { set; private get; }

        public bool IsConnect => throw new NotImplementedException();

        public string Address => throw new NotImplementedException();

        public event INetworkClientDisconnected Disconnected;

        public Task<Array2Offser> AcceptAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public void Disconnect(string user_error)
        {
            throw new NotImplementedException();
        }

        public void Send(IReleasableArray array, bool guaranteed)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(IReleasableArray array, bool guaranteed, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Сonnect(string address, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
