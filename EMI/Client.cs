using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SmartPackager;

namespace EMI
{
    using Packet;
    using Network;
    using ProBuffer;
    using MyException;

    /// <summary>
    /// Клиент EMI
    /// </summary>
    public class Client
    {
        /// <summary>
        /// RPC
        /// </summary>
        public RPC RPC { get; private set; }
        /// <summary>
        /// Подключён ли клиент
        /// </summary>
        public bool IsConnect => MyNetworkClient.IsConnect;
        /// <summary>
        /// Этот клиент на стороне сервера?
        /// </summary>
        public bool IsServerSide { get; private set; } = false;
        /// <summary>
        /// Точность синхронизации времени при подключении клиента к серверу для измерения пинга(только для клиента)
        /// </summary>
        public TimeSync.TimeSyncAccuracy TimeSyncAccuracy = TimeSync.TimeSyncAccuracy.Hight;
        /// <summary>
        /// Задержка отправки сообщений (милисекунд)
        /// </summary>
        public double Ping05 { get; private set; } = 1;
        /// <summary>
        /// Задержка отправки сообщений в секундах
        /// </summary>
        public double PingS05 => Ping05 / 1000;
        /// <summary>
        /// Пинг в милисекундах
        /// </summary>
        public double Ping => Ping05 * 2;
        /// <summary>
        /// Пинг в секундах
        /// </summary>
        public double PingS => Ping / 1000;
        /// <summary>
        /// Время после которого будет произведено отключение
        /// </summary>
        public TimeSpan PingTimeout = new TimeSpan(0, 1, 0);
        /// <summary>
        /// Вызывается если неуспешный Connect или произошло отключение
        /// </summary>
        public event INetworkClientDisconnected Disconnected;


        internal ProArrayBuffer MyArrayBuffer;
        internal ProArrayBuffer MyArrayBufferSend;

        internal InputSerialWaiter<Array2Offser> TimerSyncInputTick;
        internal InputSerialWaiter<Array2Offser> TimerSyncInputInteg;

        /// <summary>
        /// Интерфейс отправки/считывания датаграмм
        /// </summary>
        internal INetworkClient MyNetworkClient;
        /// <summary>
        /// Когда приходил прошлый запрос о пинге (для time out)
        /// </summary>
        private DateTime LastPing;
        /// <summary>
        /// Таймер времени - позволяет измерять пинг
        /// </summary>
        private TimerSync MyTimerSync;
        private CancellationTokenSource CancellationRun = new CancellationTokenSource();

        /// <summary>
        /// Инициализирует клиента но не подключает к серверу
        /// </summary>
        /// <param name="network">интерфейс подключения</param>
        /// <param name="timer">таймер времени (если null изпользует стандартный)</param>
        public Client(INetworkClient network, TimerSync timer=null)
        {
            MyNetworkClient = network;
            MyTimerSync = timer;
            RPC = new RPC();
            Init();
        }

        /// <summary>
        /// Для создания на сервере (что бы не вызывать стандартный конструктор)
        /// </summary>
        private Client()
        {
        }

        /// <summary>
        /// Для сосздания клиента на стороне сервера
        /// </summary>
        /// <param name="network"></param>
        /// <param name="timer"></param>
        /// <param name="rpc"></param>
        /// <returns></returns>
        internal static Client CreateClinetServerSide(INetworkClient network,TimerSync timer, RPC rpc)
        {
            Client client = new Client()
            {
                MyNetworkClient = network,
                MyTimerSync = timer,
                RPC = rpc,
            };
            client.IsServerSide = true;
            client.Init();
            return client;
        }

        /// <summary>
        /// Для инициализации клиента
        /// </summary>
        private void Init()
        {
            if (MyTimerSync == null)
                MyTimerSync = new TimerBuiltInSync();

            MyArrayBuffer = new ProArrayBuffer(30, 1024 * 50);
            MyArrayBufferSend = new ProArrayBuffer(30, 1024 * 50);

            TimerSyncInputTick = new InputSerialWaiter<Array2Offser>();
            TimerSyncInputInteg = new InputSerialWaiter<Array2Offser>();

            MyNetworkClient.Disconnected += LowDisconnect;
            MyNetworkClient.ProArrayBuffer = MyArrayBuffer;
        }

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="address">адрес сервера</param>
        /// <param name="token">токен отмены задачи</param>
        /// <returns>было ли произведено подключение</returns>
        public async Task<bool> Connect(string address, CancellationToken token)
        {
            if (IsConnect)
                throw new ClientAlreadyConnectException();

            try { CancellationRun.Cancel(); } catch { }
            CancellationRun = new CancellationTokenSource();

            var status = await MyNetworkClient.Сonnect(address, token).ConfigureAwait(false);

            if (status == true)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                token.Register(() => cts.Cancel());
                await TaskUtilities.InvokeAsync(() =>
                {
                    if (IsServerSide)
                    {
                        MyTimerSync.SendSync();
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        MyTimerSync.DoSync(TimeSyncAccuracy);
                    }
                }, cts).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    LowDisconnect("Сonnection canceled");
                    return false;
                }

                token.Register(() => { });
                RunProcces();

                //TODO мб тут надо что то сделать
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Закрывает соединение
        /// </summary>
        /// <param name="user_error">что сообщить клиенту при отключении</param>
        public void Disconnect(string user_error = "unknown")
        {
            MyNetworkClient.Disconnect(user_error);
        }

        /// <summary>
        /// Вызвать при внутренем отключение (так же вызывается если дисконект произошёл на более низких уровнях)
        /// </summary>
        /// <param name="error"></param>
        private void LowDisconnect(string error)
        {
            Disconnected?.Invoke(error);

            //сброс компонентов для реиспользования клиента
            if (!IsServerSide)
            {
                TimerSyncInputTick.Reset();
                TimerSyncInputInteg.Reset();
                MyArrayBuffer.Reinit();
                MyArrayBufferSend.Reinit();
            }
        }

        /// <summary>
        /// Запускает все необходимые потоки
        /// </summary>
        private void RunProcces()
        {
            TaskFactory factory = new TaskFactory(CancellationRun.Token, TaskCreationOptions.LongRunning, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
            RunProccesAccept(factory, CancellationRun.Token);
            RunProccesPing(factory, CancellationRun.Token);
        }

        /// <summary>
        /// Отвечает за отправку пинга + за отключение по ping timeout
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="token"></param>
        private void RunProccesPing(TaskFactory factory, CancellationToken token)
        {
            factory.StartNew(async () => {
                while (true)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    else
                    {
                        if (DateTime.UtcNow - LastPing > PingTimeout)
                        {
                            LowDisconnect($"Timeout (Timeout = {PingTimeout.Milliseconds} ms)");
                        }
                        else
                        {
                            IReleasableArray array = await MyArrayBufferSend.AllocateArrayAsync(Packagers.PingSizeOf, token).ConfigureAwait(false);
                            Packagers.Ping.PackUP(array.Bytes, 0, new PacketHeader(), MyTimerSync.SyncTicks);
                            await MyNetworkClient.SendAsync(array, false, token).ConfigureAwait(false);
                            array.Release();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Запускает группу потоков ProccesAccept для обработки входящих запросов
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="token"></param>
        private void RunProccesAccept(TaskFactory factory, CancellationToken token)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
            for (int i = 0; i < 20; i++)
            {
                MethodHandle handle = new MethodHandle();
                handle.Client = this;
                factory.StartNew(async () =>
                {
                    while (true)
                    {
                        await semaphore.WaitAsync(token).ConfigureAwait(false);
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        else
                        {
                            await ProccesAccept(token,handle).ConfigureAwait(false);
                            semaphore.Release();
                        }
                    }
                });
            }
        }

        private async Task ProccesAccept(CancellationToken token,MethodHandle handle)
        {
            Array2Offser data = await MyNetworkClient.Accept(token).ConfigureAwait(false);
            PacketHeader packetHeader;
            unsafe
            {
                fixed (byte* ptr = &data.Array.Bytes[data.Offset = PacketHeader.SizeOf])
                {
                    packetHeader = *(PacketHeader*)ptr;
                }
            }
            switch (packetHeader.PacketType)
            {
                case PacketType.Ping05:
                    Packagers.Ping.UnPack(data.Array.Bytes, data.Offset, out _, out long ticks);
                    Ping05 = new TimeSpan(MyTimerSync.SyncTicks - ticks).TotalMilliseconds;
                    data.Array.Release();
                    LastPing = DateTime.UtcNow;
                    break;
                case PacketType.TimeSync:
                    switch (packetHeader.TimeSyncType)
                    {
                        case TimeSyncType.Ticks:
                            TimerSyncInputTick.Set(data);
                            break;
                        case TimeSyncType.Integ:
                            TimerSyncInputInteg.Set(data);
                            break;
                        default:
                            LowDisconnect("bad package header (flags)");
                            break;
                    }
                    break;
                case PacketType.RegisterMethod:
                    switch (packetHeader.RegisterMethodType)
                    {
                        case RegisterMethodType.Request:
                            Packagers.RegisterMethodRequest.UnPack(data.Array.Bytes, data.Offset, out _, out string nameMethod);
                            if (RPC.RegisteredMethodsName.TryGetValue(nameMethod, out var ID))
                            {
                                IReleasableArray array = await MyArrayBufferSend.AllocateArrayAsync(Packagers.RegisterMethodAnswerSizeOf, token).ConfigureAwait(false);
                                Packagers.RegisterMethodAnswer.PackUP(array.Bytes, 0, new PacketHeader(PacketType.RegisterMethod, (byte)RegisterMethodType.Answer), ID);
                                await MyNetworkClient.SendAsync(array, true, token).ConfigureAwait(false);
                                array.Release();
                            }
                            else //если у нас нет такого метода
                            {
                                IReleasableArray array = await MyArrayBufferSend.AllocateArrayAsync(Packagers.RegisterMethodAnswerSizeOf, token).ConfigureAwait(false);
                                Packagers.RegisterMethodAnswer.PackUP(array.Bytes, 0, new PacketHeader(PacketType.RegisterMethod, (byte)RegisterMethodType.BadAnswer), ID);
                                await MyNetworkClient.SendAsync(array, true, token).ConfigureAwait(false);
                                array.Release();
                            }
                            break;
                        case RegisterMethodType.Answer:
                            Packagers.RegisterMethodAnswer.UnPack(data.Array.Bytes, data.Offset, out _, out ushort methodID);
                            //TODO доделать
                            break;
                        case RegisterMethodType.BadAnswer:
                            //TODO доделать мб ошибка
                            break;
                        default:
                            LowDisconnect("bad package header (flags)");
                            break;
                    }
                    data.Array.Release();
                    break;
                case PacketType.RPC:
                    switch (packetHeader.RPCType)
                    {
                        case RPCType.Simple:
                            {
                                Packagers.RPC.UnPack(data.Array.Bytes, data.Offset, out _, out ushort id, out long time);
                                data.Offset += Packagers.RPCSizeOf;

                                if (RPC.RegisteredMethods.TryGetValue(id, out var func))
                                {
                                    await func(handle, data, false, token).ConfigureAwait(false);
                                }
                            }
                            break;
                        case RPCType.Returnded:
                            {
                                Packagers.RPC.UnPack(data.Array.Bytes, data.Offset, out _, out ushort id, out long time);
                                data.Offset += Packagers.RPCSizeOf;

                                if (RPC.RegisteredMethods.TryGetValue(id, out var func))
                                {
                                    var outData = await func(handle, data, true, token).ConfigureAwait(false);
                                    if (token.IsCancellationRequested)
                                        return;
                                    Packagers.RPCAnswer.PackUP(outData.Bytes, 0, new PacketHeader(PacketType.RPC, (byte)RPCType.ReturnAnswer), id);
                                    await MyNetworkClient.SendAsync(outData, true, token).ConfigureAwait(false);
                                    outData.Release();
                                }
                            }
                            break;
                        case RPCType.ReturnAnswer:
                            {

                            }
                            break;
                        default:
                            LowDisconnect("bad package header (flags)");
                            break;
                    }
                    data.Array.Release();
                    break;
                default:
                    LowDisconnect("bad package header (PacketType)");
                    data.Array.Release();
                    break;
            }
        }
    }
}
