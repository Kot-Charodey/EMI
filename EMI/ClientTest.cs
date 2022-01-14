using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    using EMI.Lower.Accepter;
    using EMI.Lower.Buffer;
    using Lower;

    public partial class Client
    {
        internal static Client _GetClientForTest()
        {
            return new Client();
        }

        internal static Action<AcceptData> _InitAndGetProcessAccept(Client client, IMyAccepter IMyAccepter)
        {
            client.Accepter = IMyAccepter;
            client.InitAcceptLogicEvent(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 1));
            return client.ProcessAccept;
        }

        internal static PacketSendBuffer _GetPacketSendBuffer(Client client)
        {
            return client.PacketSendBuffer;
        }
    }
}
