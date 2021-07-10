using System;
using System.Threading;
using System.Diagnostics;
using SpeedByteConvector;
using System.Net;

namespace EMI
{
    using Lower.Accepter;
    using Lower.Package;


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
        /// <summary>
        /// Слушает и отправляет данные
        /// </summary>
        private IMyAccepter Accepter;
        /// <summary>
        /// Процесс отвечающий за ожидание и обработку входящих пакетов для клиента (не на сервере - там 1 общий)
        /// </summary>
        private Thread ThreadProcessLocalReceive;
        /// <summary>
        /// Измеряет пинг
        /// </summary>
        private Thread ThreadProcessPing;
        private readonly Stopwatch StopwatchPing = new Stopwatch();
        /// <summary>
        /// После ожидания произойдёт отключение
        /// </summary>
        internal const int TimeOutPing = 5000;

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
                if (Accepter.Receive(out int size).GetPacketType() == PacketType.ReqConnection1)
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

            byte[] sendBuffer = { (byte)PacketType.ReqConnection0 };

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
                var buffer = Accepter.Receive(out int size);
                ProcessAccept(buffer, size);
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
            byte[] buffer = { (byte)PacketType.ReqPing0 };

            while (IsConnect)
            {
                Accepter.Send(buffer, buffer.Length);

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Выключает поток если тот существует и запущен
        /// </summary>
        /// <param name="thread"></param>
        private static void AbortThread(Thread thread)
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
        /// что бы Stop не запустися несколько раз подряд
        /// </summary>
        private Lower.RefBool ISStopInvoke = new Lower.RefBool(false);
        /// <summary>
        /// Завершает подключение
        /// </summary>
        private void Stop()
        {
            lock (ISStopInvoke)
            {
                if (ISStopInvoke.Value == false)
                {
                    ISStopInvoke.Value = true;

                    try
                    {
                        AbortThread(ThreadProcessPing);
                        AbortThread(ThreadProcessLocalReceive);
                        AbortThread(ThreadRequestLostPackages);
                        StopwatchPing.Stop();

                        Accepter.Stop();
                        IsConnect = false;

                        ReturnWaiter.ErrorStop();

                        CloseEvent?.Invoke(CloseReason);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ошибка в коде EMI: Client->Stop:\n" + e);
                    }
                }
            }
        }


        /// <summary>
        /// отправляет причину ошибки оппоненту
        /// </summary>
        /// <param name="closeType">ЕГО ОШИБКА см описание кодов CloseType</param>
        private void SendErrorClose(CloseType closeType)
        {
            Console.WriteLine("SendErrorClose -> " + closeType);
            CloseReason = closeType - 1;
            if (CloseReason == CloseType.None)
            {
                throw new Exception("Ошибка в коде EMI: неправильный тип <CloseType>");
            }

            byte[] sendBuffer = { (byte)PacketType.SndClose, (byte)closeType };
            //отправим 10 раз что бы точно дошло
            for (int i = 0; i < 10; i++)
            {
                Accepter.Send(sendBuffer, sendBuffer.Length);
                Thread.Sleep(100);
            }
        }
    }
}