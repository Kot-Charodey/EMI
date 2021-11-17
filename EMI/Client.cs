﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

namespace EMI
{
    using Lower;
    using Lower.Accepter;
    using Lower.Package;

    public partial class Client
    {
        /// <summary>
        /// Локальный список вызываймых методов
        /// </summary>
        public RPC RPC { get; private set; }
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
        internal const int TimeOutPing = 6000;
        /// <summary>
        /// Чатота опроса ping
        /// </summary>
        public Ping.RequestRate RequestRatePing = EMI.Ping.RequestRate.ms1000;
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
            RPC = new RPC(this);
        }
        
        /// <summary>
        /// Инициализация клиента на стороне сервера
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="accepter"></param>
        internal Client(EndPoint endPoint, MultiAccepter accepter)
        {
            RPC = new RPC(this);

            IsConnect = true;

            //AccepterDone нужно запустить после инициализации InitAcceptLogicEvent
            Action AccepterDone = MultyAccepterClient.Create(out var multyAccepterClient, endPoint, accepter, this, ProcessAccept);
            Accepter = multyAccepterClient;
            InitAcceptLogicEvent(endPoint);

            AccepterDone.Invoke();

            ThreadRequestLostPackages.Start();
            ProcessPingSenderStart();
        }

        /// <summary>
        /// Попытка подключиться к серверу (если вернёт null то не успешная)
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static async Task<Client> Connect(IPAddress IP, ushort port)
        {
            Client client = new Client();
            var EndPoint = new IPEndPoint(IP, port);
            client.Accepter = new SimpleAccepter(EndPoint);

            bool good = await client.ConnectProcess();

            if (!good)
            {
                client.SendErrorClose(CloseType.StopConnectionError);
                client.Accepter.Stop();

                return null;
            }


            client.IsConnect = true;

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
        private async Task<bool> ConnectProcess()
        {
            byte[] snd1 = { (byte)PacketType.ReqConnection0 };

            byte[] buffer = new byte[MTU.Size];
            int size;

            CancellationToken cancellation = new CancellationTokenSource(30000).Token;

            while (!cancellation.IsCancellationRequested)
            {
                bool CancelSpam = false;
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                Task.Run(() =>
                {
                    while (!CancelSpam)
                    {
                        Accepter.Send(snd1, snd1.Length);
                        Task.Delay(50).Wait();
                    }
                });
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен

                bool accept = await TaskUtilities.InvokeAsync(() =>
                {
                    size = Accepter.Receive(buffer);
                    if (buffer.GetPacketType() == PacketType.ReqConnection1 && size == sizeof(PacketType) + sizeof(int))
                    {
                        buffer[0] = (byte)PacketType.ReqConnection2;
                        Accepter.Send(buffer, size);
                    }
                }, new CancellationTokenSource(500));

                CancelSpam = true;

                if (accept) //если попытка считается успешной то проверяем отправляет ли нам что либо сервер
                {
                    bool accept2 = await TaskUtilities.InvokeAsync(() =>
                    {
                        while (true)
                        {
                            Accepter.Receive(buffer);
                            if (buffer.GetPacketType() == PacketType.ReqPing0)
                            {
                                break;
                            }
                        }
                    }, new CancellationTokenSource(4000));

                    if (accept2)
                        return true;
                }
            }

            return false;
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
            BufferedObjectStore<byte[]> bufferStore = new BufferedObjectStore<byte[]>(Environment.ProcessorCount, () => new byte[MTU.Size]);

            while (IsConnect)
            {
                var handle = bufferStore.GetObject();
                int size = Accepter.Receive(handle.Object);
                var acceptD = new AcceptData(size, handle.Object);
                Task.Run(() =>
                {
                    ProcessAccept(acceptD);
                    handle.Release();
                });
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
            var delay = EMI.Ping.RequestRateDelay;
            while (IsConnect)
            {
                Accepter.Send(buffer, buffer.Length);

                Thread.Sleep(delay[(int)RequestRatePing]);
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
        private readonly RefVarible<bool> ISStopInvoke = new RefVarible<bool>(false);
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

                    //запускаем в новом потоке что бы мы нечайно не завершили самих себя
                    new Thread(() =>
                    {
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
                    }).Start();
                }
            }
        }


        /// <summary>
        /// отправляет причину ошибки оппоненту
        /// </summary>
        /// <param name="closeType">ЕГО ОШИБКА см описание кодов CloseType</param>
        private void SendErrorClose(CloseType closeType)
        {
            CloseReason = closeType - 1;
#if DEBUG
            Console.WriteLine("SendErrorClose -> " + closeType);
#endif
            if (CloseReason == CloseType.None)
            {
                throw new Exception("Ошибка в коде EMI: неправильный тип <CloseType>");
            }

            byte[] sendBuffer = { (byte)PacketType.SndClose, (byte)closeType };

            try
            {
                Accepter.Send(sendBuffer, sendBuffer.Length);
            }
            catch
            {

            }
        }
    }
}