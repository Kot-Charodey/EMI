using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace EMI.Lower.Accepter
{
    internal class MultyAccepterClient : IMyAccepter
    {
        public bool Stopped { get; private set; } = false;
        public EndPoint EndPoint { get; private set; }
        private readonly MultiAccepter MultiAccepter;
        public Action<byte[]> AcceptEvent;

        public MultyAccepterClient(EndPoint address,MultiAccepter multiAccepter,Action<byte[]> acceptEvent)
        {
            MultiAccepter = multiAccepter;
            EndPoint = address;
            AcceptEvent = acceptEvent;

            lock (MultiAccepter.ReceiveClients)
            {
                MultiAccepter.ReceiveClients.Add(this);
            }
        }

        public byte[] Receive()
        {
            throw new Exception("use Action acceptEvent");
        }

        public void Send(byte[] buffer, int count)
        {
            MultiAccepter.Client.SendTo(buffer, count, SocketFlags.None, EndPoint);
        }

        public void Stop()
        {
            Stopped = true;
        }
    }
}
