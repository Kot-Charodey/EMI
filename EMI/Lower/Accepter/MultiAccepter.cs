using SpeedByteConvector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower.Accepter
{
    using Package;
    using System.Linq;

    /// <summary>
    /// Принимает и обрабатывает запросы на стороне сервера
    /// </summary>
    internal class MultiAccepter
    {

        public Socket Socket;
        public Dictionary<EndPoint, MultyAccepterClient> ReceiveClients = new Dictionary<EndPoint, MultyAccepterClient>();
        public Dictionary<EndPoint, DateTime> RegisteringСlients = new Dictionary<EndPoint, DateTime>();
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private Action<Client> AcceptEvent;

        public MultiAccepter(int port)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };
            Socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public void StartProcessReceive(Action<Client> acceptEvent)
        {
            AcceptEvent = acceptEvent;
            Task.Factory.StartNew(()=>ProcessReceive(CancellationTokenSource.Token), CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private const int BufferSize = 1248;//максимальный размер MPU
        private byte[] Buffer;

        /// <summary>
        /// Входная точка обработки и запуска RPC на сервере
        /// </summary>
        public void ProcessReceive(CancellationToken cancellationToken)
        {
            Buffer = new byte[BufferSize];
            EndPoint receivePoint = new IPEndPoint(IPAddress.Any, 1);
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    int size = Socket.ReceiveFrom(Buffer, ref receivePoint);

                    DateTime nowTime = DateTime.Now;
                    var removingTimeOut = from point in RegisteringСlients where (nowTime - point.Value).Seconds > 15 select point.Key;
                    foreach(var key in removingTimeOut)
                    {
                        RegisteringСlients.Remove(key);
                    }

                    lock (ReceiveClients) {
                        if (ReceiveClients.TryGetValue(receivePoint, out MultyAccepterClient client))
                        {
                            //RPC
                            client.AcceptEvent.Invoke(Buffer, size);
                        }
                        else
                        {
                            PacketType packetType = Buffer.GetPacketType();

                            //Если игрок хочет подключиться
                            if (packetType == PacketType.ReqConnection0 || packetType == PacketType.ReqConnection2)
                            {
                                if (RegisteringСlients.TryGetValue(receivePoint, out DateTime time))
                                {
                                    //ждём ответа в виде хэш кода который мы отсылаем
                                    if (packetType == PacketType.ReqConnection2 && size == sizeof(PacketType) + sizeof(int))
                                    {
                                        PackConvector.UnPack<PacketType, int>(Buffer, out _, out int hash);
                                        if(hash == receivePoint.GetHashCode())
                                        {
                                            RegisteringСlients.Remove(receivePoint);
                                            Task.Run(()=>
                                            {
                                                Client cc = new Client(receivePoint, this);
                                                AcceptEvent.Invoke(cc);
                                            });
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    RegisteringСlients.Add(receivePoint, DateTime.Now);

                                    byte[] sendBuffer = new byte[sizeof(PacketType) + sizeof(int)];
                                    PackConvector.PackUP(sendBuffer, PacketType.ReqConnection1, receivePoint.GetHashCode());
                                    Socket.SendTo(sendBuffer, receivePoint);
                                }
                            }
                        }
                    }
                }
                catch 
                {
                    //если задача отменена
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;
                }
            }
        }


        /// <summary>
        /// Отключает всех клиентов
        /// </summary>
        public void Stop()
        {
            //TODO
            //for (int i = 0; i < ReceiveClients.Count; i++)
            //{
            //    lock (ReceiveClients)
            //    {
            //        if (ReceiveClients[i].Stopped == false)
            //        {
            //            ReceiveClients[i].Client.Close();
            //        }
            //    }
            //}

            //завершает потоки обработки
            CancellationTokenSource.Cancel();
            Socket.Close();
        }
    }
}