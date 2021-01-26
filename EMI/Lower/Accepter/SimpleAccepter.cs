using System;
using System.Net.Sockets;
using System.Net;

namespace EMI.Lower.Accepter
{
    internal class SimpleAccepter : IMyAccepter
    {
        private UdpClient UdpClient;
        private IPEndPoint EndPoint;

        public SimpleAccepter(IPEndPoint address)
        {
            UdpClient = new UdpClient();
        }

        private IPEndPoint Point = new IPEndPoint(IPAddress.Any, 1);
        private byte[] tmp;

        public byte[] Receive()
        {
            @while:
            tmp = UdpClient.Receive(ref Point);

            if (Point.Equals(EndPoint))
            {
                return tmp;
            }
            goto @while;
        }

        public void Send(byte[] buffer, int count)
        {
            UdpClient.Send(buffer, count, EndPoint);
        }
    }
}
