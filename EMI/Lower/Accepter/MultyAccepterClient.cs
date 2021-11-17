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
        public Action<AcceptData> AcceptEvent;

        private MultyAccepterClient(EndPoint address,MultiAccepter multiAccepter,Client client, Action<AcceptData> acceptEvent)
        {
            Client = client;
            MultiAccepter = multiAccepter;
            EndPoint = address;
            AcceptEvent = acceptEvent;
        }

        /// <summary>
        /// Создаёт серверного клиента
        /// </summary>
        /// <param name="сlientAccepter">Серверный клиент</param>
        /// <param name="address"></param>
        /// <param name="multiAccepter"></param>
        /// <param name="client"></param>
        /// <param name="acceptEvent"></param>
        /// <returns>Делегат для активации ацептера (следует запускать после инициализации клиента)</returns>
        public static Action Create(out MultyAccepterClient сlientAccepter, EndPoint address, MultiAccepter multiAccepter, Client client, Action<AcceptData> acceptEvent)
        {
            var mac = new MultyAccepterClient(address, multiAccepter, client, acceptEvent);
            сlientAccepter = mac;

            return () =>
            {
                lock (mac.MultiAccepter.ReceiveClients)
                {
                    mac.MultiAccepter.ReceiveClients.Add(mac.EndPoint, mac);
                }
            };
        }

        public int Receive(byte[] buffer)
        {
            //на серверных клиента обработчик вызывается классе MultiAccepter
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