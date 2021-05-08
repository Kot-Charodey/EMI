﻿using System;
using System.Net.Sockets;
using System.Net;

namespace EMI.Lower.Accepter
{
    internal class SimpleAccepter : IMyAccepter
    {
        private readonly Socket Client;
        public EndPoint EndPoint { get; private set; }

        public SimpleAccepter(IPEndPoint address)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };
            Client.Bind(new IPEndPoint(IPAddress.Any, address.Port));
            EndPoint = address;
        }

        private EndPoint Point = new IPEndPoint(IPAddress.Any, 1);
        private readonly byte[] tmp=new byte[1248];

        public byte[] Receive()
        {
            @while:
            Client.ReceiveFrom(tmp,ref Point);

            if (Point.Equals(EndPoint))
            {
                return tmp;
            }
            goto @while;
        }

        public void Send(byte[] buffer, int count)
        {
            Client.SendTo(buffer, count,SocketFlags.None, EndPoint);
        }

        public void Stop()
        {
            Client.Dispose();
        }
    }
}
