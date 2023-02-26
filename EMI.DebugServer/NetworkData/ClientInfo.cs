using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EMI.DebugServer.NetworkData
{
    public struct ClientInfo
    {
        public Guid ClientID;
        public string Address;

        public bool IsConnect;
        public bool IsServerSize;
        public TimeSpan Ping;
        public TimeSpan PingTimeout;
        public int MaxPacketAcceptSize;

        public ClientInfo(Client client,Guid guid)
        {
            ClientID = guid;
            Address = client.RemoteAddress;
            IsConnect = client.IsConnect;
            IsServerSize = client.IsServerSide;
            Ping = client.Ping;
            PingTimeout = client.PingTimeout;
            MaxPacketAcceptSize = client.MaxPacketAcceptSize;
        }
    }
}
