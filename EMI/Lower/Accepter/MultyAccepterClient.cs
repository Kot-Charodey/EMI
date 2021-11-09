using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace EMI.Lower.Accepter
{
    internal class MultyAccepterClient : IMyAccepter
    {
        public Client Client { get; private set; }
        public bool Stopped { get; private set; } = false;
        public EndPoint EndPoint { get; private set; }
        private readonly MultiAccepter MultiAccepter;
        public Func<AcceptData,Task> AcceptEvent;

        public MultyAccepterClient(EndPoint address,MultiAccepter multiAccepter,Client client, Func<AcceptData, Task> acceptEvent)
        {
            Client = client;
            MultiAccepter = multiAccepter;
            EndPoint = address;
            AcceptEvent = acceptEvent;

            lock (MultiAccepter.ReceiveClients)
            {
                MultiAccepter.ReceiveClients.Add(EndPoint,this);
            }
        }

        public byte[] Receive(out int size)
        {
            throw new Exception("use Action acceptEvent");
        }

        public void Send(byte[] buffer, int count)
        {
            MultiAccepter.Socket.SendTo(buffer, count, SocketFlags.None, EndPoint);
        }

        /// <summary>
        /// Вызывается клиентом при отключении
        /// </summary>
        public void Stop()
        {
            if (!Stopped)
            {
                Stopped = true;

                lock (MultiAccepter.ReceiveClients)
                {
                    MultiAccepter.ReceiveClients.Remove(EndPoint);
                }
#if DEBUG
                Console.WriteLine($"Client removed [{EndPoint}]");
#endif
            }
        }
    }
}