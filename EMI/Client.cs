using System;
using System.Threading;
using System.Diagnostics;
using SpeedByteConvector;
using System.Net;

namespace EMI
{
    using Lower;
    using Lower.Accepter;


    public partial class Client
    {
        /// <summary>
        /// Локальный список вызываймых методов
        /// </summary>
        public RPC RPC { get; private set; } = new RPC();
        /// <summary>
        /// Уровень привилегий (влиет на возможность выполнить RPC запрос)
        /// </summary>
        public byte LVL_Permission;

        private IMyAccepter Accepter;
        private Thread ThreadProcessLocalReceive;
        private Thread ThreadProcessPing;
        private readonly Stopwatch StopwatchPing = new Stopwatch();
        /// <summary>
        /// После ожидания произойдёт отключение
        /// </summary>
        internal const int TimeOutPing = 15000;

        /// <summary>
        /// Существует ли подключение между клиентами
        /// </summary>
        public bool IsConnect { get; private set; }
        /// <summary>
        /// Содержит причину разрыва соединения
        /// </summary>
        public CloseType CloseReason { get; private set; }
        /// <summary>
        /// Событие происходит при отключении клиента
        /// </summary>
        public event Action<CloseType> CloseEvent;

        /// <summary>
        /// пинг в секундах
        /// </summary>
        public double Ping { get; private set; } = 0.001;
        /// <summary>
        /// пинг в миллисекундах
        /// </summary>
        public int PingIMS { get; private set; } = 1;
        /// <summary>
        /// пинг в миллисекундах
        /// </summary>
        public double PingMS { get; private set; } = 1;

        private Client()
        {
        }

        internal Client(EndPoint endPoint, MultiAccepter accepter)
        {
            IsConnect = true;
            InitAcceptLogicEvent(endPoint);

            MultyAccepterClient multyAccepterClient = new MultyAccepterClient(endPoint, accepter, ProcessAccept);
            Accepter = multyAccepterClient;
            ThreadRequestLostPackages.Start();
            ProcessPingSenderStart();
        }

        /// <summary>
        /// Попытка подключиться к серверу (если вернёт null то не успешная)
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Client Connect(IPAddress IP, ushort port)
        {
            Client client = new Client();
            var EndPoint = new IPEndPoint(IP, port);
            client.Accepter = new SimpleAccepter(EndPoint);

            const double timeOut = 30;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Thread thread = new Thread(client.ConnectProcess)
            {
                IsBackground = true,
                Name = "EMI.ConnectProcess"
            };
            thread.Start();

            while (thread.IsAlive)
            {
                if (stopwatch.Elapsed.TotalSeconds > timeOut)
                {
                    stopwatch.Stop();
                    thread.Abort();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

            if (client.IsConnect == false)
            {
                client.SendErrorClose(CloseType.StopConnectionError);
                
                return null;
            }

            client.InitAcceptLogicEvent(EndPoint);
            client.StartProcessLocalReceive();
            client.ThreadRequestLostPackages.Start();
            client.ProcessPingSenderStart();
            return client;
        }

        /// <summary>
        /// Отключает клиента
        /// </summary>
        public void Close()
        {
            SendErrorClose(CloseType.NormalStop);
            Stop();
        }

        /// <summary>
        /// Попытка подключиться к серверу
        /// </summary>
        private void ConnectProcess()
        {
            bool Fail = false;

            Thread threadReader = new Thread(() =>
              {
                  byte[] data = Accepter.Receive();
                  BitPacketSimple reqP = PackConvector.UnPackJust<BitPacketSimple>(data);
                  if (reqP.PacketType == PacketType.ReqConnectionGood)
                  {

                  }
                  else
                  {
                      Fail = true;
                  }
              })
            {
                IsBackground = true
            };
            threadReader.Start();

            BitPacketSimple sendP = new BitPacketSimple(PacketType.ReqConnection);
            byte[] sendBuffer = sendP.GetAllBytes();

            while (threadReader.IsAlive)
            {
                Accepter.Send(sendBuffer, sendBuffer.Length);
                Thread.Sleep(50);
            }

            if (Fail)
            {
                return;
            }

            IsConnect = true;
        }

        private void StartProcessLocalReceive()
        {
            ThreadProcessLocalReceive = new Thread(ProcessLocalReceive)
            {
                IsBackground = true,
                Name = "EMI.Client.ThreadProcessLocalReceive [" + Accepter.EndPoint.ToString() + "]"
            };
            ThreadProcessLocalReceive.Start();
        }

        private void ProcessLocalReceive()
        {
            while (IsConnect)
            {
                ProcessAccept(Accepter.Receive());
            }
        }

        private void ProcessPingSenderStart()
        {
            ThreadProcessPing = new Thread(ProcessPingSender)
            {
                IsBackground = true,
                Name = "EMI.Client.ThreadProcessPingSender [" + Accepter.EndPoint.ToString() + "]"
            };
            ThreadProcessPing.Start();
        }

        private void ProcessPingSender()
        {
            StopwatchPing.Start();
            byte[] buffer;
            BitPacketSimple bitPacket = new BitPacketSimple(PacketType.ReqPing0);
            buffer = bitPacket.GetAllBytes();

            while (IsConnect)
            {
                Accepter.Send(buffer, buffer.Length);

                Thread.Sleep(100);
            }
        }

        private static void AbortTH(Thread thread)
        {
            try
            {
                if (thread != null && thread.IsAlive)
                {
                    thread.Abort();
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Завершает подключение
        /// </summary>
        private void Stop()
        {
            AbortTH(ThreadProcessPing);
            AbortTH(ThreadProcessLocalReceive);
            AbortTH(ThreadRequestLostPackages);
            StopwatchPing.Stop();

            Accepter.Stop();
            IsConnect = false;

            ReturnWaiter.ErrorStop();

            CloseEvent?.Invoke(CloseReason);
        }

        private void SendErrorClose(CloseType closeType)
        {
            CloseReason = closeType - 1;
            var bitPacketSimple = new BitPacketSimple(PacketType.SndClose, new byte[] { (byte)closeType });
            byte[] sendBuffer = bitPacketSimple.GetAllBytes();
            Accepter.Send(sendBuffer, sendBuffer.Length);
        }
    }
}
