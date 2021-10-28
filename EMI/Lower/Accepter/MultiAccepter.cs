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
        public Server Server;
        public Socket Socket;
        public Dictionary<EndPoint, MultyAccepterClient> ReceiveClients = new Dictionary<EndPoint, MultyAccepterClient>();
        public Dictionary<EndPoint, DateTime> RegisteringСlients = new Dictionary<EndPoint, DateTime>();
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private Action<Client> AcceptEvent;

        public MultiAccepter(int port,Server server)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };
            Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            Server = server;
        }

        public void StartProcessReceive(Action<Client> acceptEvent)
        {
            AcceptEvent = acceptEvent;
            System.Threading.Tasks.Task.Factory.StartNew(()=> ProcessReceive(CancellationTokenSource.Token), CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int size = Socket.ReceiveFrom(Buffer, ref receivePoint);

                    DateTime nowTime = DateTime.Now;
                    var removingTimeOut = from point in RegisteringСlients where (nowTime - point.Value).Seconds > 60 select point.Key;
                    foreach(var key in removingTimeOut)
                    {
                        RegisteringСlients.Remove(key);
                    }

                    lock (ReceiveClients) {
                        if (ReceiveClients.TryGetValue(receivePoint, out MultyAccepterClient client))
                        {
                            //TODO проверить многопоточность
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
                                    switch (packetType)
                                    {
                                        case PacketType.ReqConnection0:
                                            SendReqCon1(receivePoint);
                                            break;
                                        case PacketType.ReqConnection2:
                                            //ждём ответа в виде хэш кода который мы отсылаем
                                            if (packetType == PacketType.ReqConnection2 && size == sizeof(PacketType) + sizeof(int))
                                            {
                                                int hash;
                                                unsafe
                                                {
                                                    fixed (byte* num = &Buffer[1])
                                                        hash = *(int*)num;
                                                }
                                                if (hash == receivePoint.GetHashCode())
                                                {
                                                    RegisteringСlients.Remove(receivePoint);
                                                    Task.Run(() =>
                                                    {
                                                        Client cc = new Client(receivePoint, this);
                                                        AcceptEvent.Invoke(cc);
                                                    });
                                                    continue;
                                                }
                                            }
                                            break;
                                        default:
                                            RegisteringСlients.Remove(receivePoint);
                                            break;
                                    }
                                }
                                else
                                {
                                    RegisteringСlients.Add(receivePoint, DateTime.Now);
                                    SendReqCon1(receivePoint);
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

        private byte[] sendBuffer = {(byte)PacketType.ReqConnection1,0,0,0,0};

        private void SendReqCon1(EndPoint receivePoint)
        {
            unsafe
            {
                    fixed (byte* num = &sendBuffer[1])
                        *(int*)num = receivePoint.GetHashCode();
            }
            Socket.SendTo(sendBuffer, receivePoint);
        }


        /// <summary>
        /// Отключает всех клиентов
        /// </summary>
        public void Stop()
        {
            lock (ReceiveClients)
            {
                foreach(var client in ReceiveClients.Values)
                {
                    if (!client.Stopped)
                    {
                        client.Client.Close();
                    }
                }
            }

            //завершает потоки обработки
            CancellationTokenSource.Cancel();
            Socket.Close();
        }
    }
}