using SpeedByteConvector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EMI.Lower.Accepter
{
    using Package;

    internal class MultiAccepter
    {
        public Socket Client;
        public List<MultyAccepterClient> ReceiveClients = new List<MultyAccepterClient>();
        public Thread Thread;
        private Action<Client> AcceptEvent;

        public MultiAccepter(int port)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };
            Client.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public void StartProcessReceive(Action<Client> acceptEvent)
        {
            AcceptEvent = acceptEvent;
            Thread = new Thread(ProcessReceive)
            {
                IsBackground = true,
                Name = "EMI.ProcessReceive"
            };
            Thread.Start();
        }

        private EndPoint Point = new IPEndPoint(IPAddress.Any, 1);
        private const int BufferSize = 1248;//максимальный размер MPU
        private byte[] Buffer;

        private bool TryGetValue(out MultyAccepterClient mac)
        {
            for (int i = 0; i < ReceiveClients.Count; i++)
            {
                if (ReceiveClients[i].EndPoint.Equals(Point))
                {
                    mac = ReceiveClients[i];
                    return true;
                }
            }

            mac = null;
            return false;
        }

        public void ProcessReceive()
        {
            @while:
            try
            {
                Buffer = new byte[BufferSize];
                int size = Client.ReceiveFrom(Buffer, ref Point);

                lock (ReceiveClients)
                {
                    //убирает отключённых клиентов

                    for (int i = 0; i < ReceiveClients.Count; i++)
                    {
                        if (ReceiveClients[i].Stopped)
                        {
#if DEBUG
                            Console.WriteLine($"Client removed [{ReceiveClients[i].EndPoint}]");
#endif
                            ReceiveClients.RemoveAt(i);
                            i--;
                        }
                    }

                    if (TryGetValue(out MultyAccepterClient mac))
                    {
                        mac.AcceptEvent.Invoke(Buffer, size);
                    }
                    else
                    {
                        try
                        {
                            if (BitPacketsUtilities.GetPacketType(Buffer) == PacketType.ReqConnection0)
                            {
                                byte[] sendBuffer = new byte[sizeof(PacketType)];
                                PackConvector.PackUP(sendBuffer, PacketType.ReqConnection1);

                                for (int i = 0; i < 5; i++)
                                {
                                    Client.SendTo(sendBuffer, Point);
                                    Thread.Sleep(20);
                                }

                                Client client = new Client(Point, this);
                                Thread invoker = new Thread(() =>
                                {
                                    AcceptEvent.Invoke(client);
                                })
                                {
                                    IsBackground = true,
                                    Name = "EMI.Client [" + Point.ToString() + "]"
                                };
                                invoker.Start();

                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch
            {

            }
            goto @while;
        }

        public void Stop()
        {
            for (int i = 0; i < ReceiveClients.Count; i++)
            {
                lock (ReceiveClients)
                {
                    if (ReceiveClients[i].Stopped == false)
                    {
                        try
                        {
                            ReceiveClients[i].Stop();
                        }
                        catch
                        {

                        }
                    }
                }
            }

            try
            {
                Thread.Abort();
            }
            catch
            {
            }
            Client.Dispose();
        }
    }
}