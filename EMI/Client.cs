using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SmartPackager;

namespace EMI
{
    using Packet;
    using Network;
    using ProBuffer;
    using Indicators;
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
        /// Задержка отправки сообщений (не путать с ping (ping = Ping05 * 2)) (обновляется раз в секунду)
        /// </summary>
        public TimeSpan Ping05 = new TimeSpan(0);
        /// <summary>
        /// Время после которого будет произведено отключение
        /// </summary>
        public TimeSpan PingTimeout = new TimeSpan(0, 1, 0);
        /// <summary>
        /// Вызывается если неуспешный Connect или произошло отключение
        /// </summary>
        public event INetworkClientDisconnected Disconnected;

        //выделяет массивы
        internal ProArrayBuffer MyArrayBuffer;
        internal ProArrayBuffer MyArrayBufferSend;

        //для работы TimerSync
        internal InputSerialWaiter<Array2Offser> TimerSyncInputTick;
        internal InputSerialWaiter<Array2Offser> TimerSyncInputInteg;

        internal RPCAdressing MyRPCAdressing;

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
        private List<WaitHandle> RPCReturn;
        /// <summary>
        /// Токен вызывающий сброс когда получен и обработан SendList
        /// </summary>
        private CancellationTokenSource WaitAcceptSendList;


        /// <summary>
        /// Инициализирует клиента но не подключает к серверу
        /// </summary>
        /// <param name="network">интерфейс подключения</param>
        /// <param name="timer">таймер времени (если null изпользует стандартный)</param>
        public Client(INetworkService network, TimerSync timer = null)
        {
            MyNetworkClient = network.GetNewClient();
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
        internal static Client CreateClinetServerSide(INetworkClient network, TimerSync timer, RPC rpc)
        {
            Client client = new Client()
            {
                MyNetworkClient = network,
                MyTimerSync = timer,
                RPC = rpc,
            };
            client.IsServerSide = true;
            client.Init();

            rpc.DoRegisterMethod += client.RPCReg;
            rpc.DoUnregisterMethod += client.RPCUNreg;

            client.WaitAcceptSendList = new CancellationTokenSource();

            client.RunProcces();
            client.MyTimerSync.SendSync(); 

            client.SendRPCList();

            try { Task.Delay(-1, client.WaitAcceptSendList.Token).Wait(); } catch { }

            return client;
        }

        /// <summary>
        /// Для инициализации клиента
        /// </summary>
        private void Init()
        {
            if (MyTimerSync == null)
                MyTimerSync = new TimerBuiltInSync(this);

            MyArrayBuffer = new ProArrayBuffer(30, 1024 * 50);
            MyArrayBufferSend = new ProArrayBuffer(30, 1024 * 50);

            TimerSyncInputTick = new InputSerialWaiter<Array2Offser>();
            TimerSyncInputInteg = new InputSerialWaiter<Array2Offser>();
            RPCReturn = new List<WaitHandle>();

            MyRPCAdressing = new RPCAdressing();

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
                throw new AlreadyException();

            try { CancellationRun.Cancel(); } catch { }
            CancellationRun = new CancellationTokenSource();
            
            var status = await MyNetworkClient.Сonnect(address, token).ConfigureAwait(false);

            if (status == true)
            {
                RPC.DoRegisterMethod += RPCReg;
                RPC.DoUnregisterMethod += RPCUNreg;

                

                CancellationTokenSource cts = new CancellationTokenSource();

                if (WaitAcceptSendList != null)
                    WaitAcceptSendList.Dispose();
                WaitAcceptSendList = new CancellationTokenSource();

                token.Register(() => TaskUtilities.Try(() => cts.Cancel()));
                token.Register(() => TaskUtilities.Try(() => WaitAcceptSendList.Cancel()));


                RunProcces();

                if (token.IsCancellationRequested)
                {
                    MyNetworkClient.Disconnect("Сonnection canceled");
                    return false;
                }

                await TaskUtilities.InvokeAsync(() =>
                {
                    MyTimerSync.DoSync(TimeSyncAccuracy);
                }, cts).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    MyNetworkClient.Disconnect("Сonnection canceled");
                    return false;
                }

                Console.WriteLine("wait ...");
                    SendRPCList();
                //ожидат завершение синхронизации SendList
                try { await Task.Delay(-1, WaitAcceptSendList.Token).ConfigureAwait(false);}catch { }

                Console.WriteLine("done");

                if (token.IsCancellationRequested)
                {
                    MyNetworkClient.Disconnect("Сonnection canceled");
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void SendRPCList()
        {
            lock (RPC)
            {
                List<ushort> IDs = new List<ushort>();
                List<string> names = new List<string>();

                for (ushort i = 0; i < RPC.RegisteredMethodsName.Length; i++)
                {
                    if (RPC.RegisteredMethodsName[i] != null)
                    {
                        IDs[i] = i;
                        names[i] = RPC.RegisteredMethodsName[i];
                    }
                }

                var array = new WrapperArray(Packagers.RegisterMethodList.PackUP(new PacketHeader(RegisterMethodType.SendList), IDs.ToArray(), names.ToArray()));
                MyNetworkClient.Send(array, true);
                array.Release();
            }
        }

        private void RPCReg(string name, ushort id)
        {
            Task.Run(async () =>
            {
                var array = await MyArrayBufferSend.AllocateArrayAsync((int)Packagers.RegisterMethodAdd.CalcNeedSize(default, default, name), default).ConfigureAwait(false);
                Packagers.RegisterMethodAdd.PackUP(array.Bytes, 0, new PacketHeader(RegisterMethodType.Add), id, name);
                await MyNetworkClient.SendAsync(array, true, default).ConfigureAwait(false);
                array.Release();
            });
        }

        private void RPCUNreg(ushort id)
        {
            Task.Run(async () =>
            {
                var array = await MyArrayBufferSend.AllocateArrayAsync(Packagers.RegisterMethodRemoveSizeOf,default).ConfigureAwait(false);
                Packagers.RegisterMethodRemove.PackUP(array.Bytes, 0, new PacketHeader(RegisterMethodType.Remove), id);
                await MyNetworkClient.SendAsync(array, true, default).ConfigureAwait(false);
                array.Release();
            });
        }

        /// <summary>
        /// Закрывает соединение
        /// </summary>
        /// <param name="user_error">что сообщить клиенту при отключении</param>
        public void Disconnect(string user_error = "unknown")
        {
            if (!IsConnect)
            {
                throw new AlreadyException();
            }
            MyNetworkClient.Disconnect(user_error);
        }

        /// <summary>
        /// Вызвать при внутренем отключение
        /// </summary>
        /// <param name="error"></param>
        private void LowDisconnect(string error)
        {
            Disconnected?.Invoke(error);

            TaskUtilities.Try(() => RPC.DoRegisterMethod -= RPCReg);
            TaskUtilities.Try(() => RPC.DoUnregisterMethod -= RPCUNreg);

            //сброс компонентов для реиспользования клиента
            if (!IsServerSide)
            {
                TimerSyncInputTick.Reset();
                TimerSyncInputInteg.Reset();
                MyArrayBuffer.Reinit();
                MyArrayBufferSend.Reinit();
                MyRPCAdressing.Reinit();
                RPCReturn.Clear();
            }
        }

        /// <summary>
        /// Запускает все необходимые потоки
        /// </summary>
        private void RunProcces()
        {
            TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
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
            factory.StartNew(async () =>
            {
                LastPing = DateTime.UtcNow;
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
                            MyNetworkClient.Disconnect($"Timeout (Timeout = {PingTimeout.Milliseconds} ms)");
                        }
                        else
                        {
                            IReleasableArray array = await MyArrayBufferSend.AllocateArrayAsync(Packagers.PingSizeOf, token).ConfigureAwait(false);
                            Packagers.Ping.PackUP(array.Bytes, 0, new PacketHeader(PacketType.Ping05), MyTimerSync.SyncTicks);
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
            SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
            for (int i = 0; i < 5; i++)
            {
                MethodHandle handle = new MethodHandle
                {
                    Client = this
                };
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
                            await ProccesAccept(token, handle).ConfigureAwait(false);
                            semaphore.Release();
                        }
                    }
                });
            }
        }

        private async Task ProccesAccept(CancellationToken token, MethodHandle handle)
        {
            Array2Offser data = await MyNetworkClient.AcceptAsync(token).ConfigureAwait(false);
            PacketHeader packetHeader;
            unsafe
            {
                fixed (byte* ptr = &data.Array.Bytes[data.Offset])
                {
                    packetHeader = *(PacketHeader*)ptr;
                }
            }

            if ((byte)packetHeader.PacketType > (byte)PacketType.TimeSync)
                Console.WriteLine(packetHeader.PacketType);
            switch (packetHeader.PacketType)
            {
                case PacketType.Ping05:
                    Packagers.Ping.UnPack(data.Array.Bytes, data.Offset, out _, out long ticks);
                    Ping05 = new TimeSpan(MyTimerSync.SyncTicks - ticks);
                    data.Array.Release();
                    LastPing = DateTime.UtcNow;
                    break;
                case PacketType.TimeSync:
                    switch ((TimeSyncType)packetHeader.Flags)
                    {
                        case TimeSyncType.Ticks:
                            TimerSyncInputTick.Set(data);
                            break;
                        case TimeSyncType.Integ:
                            TimerSyncInputInteg.Set(data);
                            break;
                        default:
                            MyNetworkClient.Disconnect("bad package header (flags)");
                            break;
                    }
                    break;
                case PacketType.RegisterMethod:
                    switch ((RegisterMethodType)packetHeader.Flags)
                    {
                        case RegisterMethodType.Add:
                            Packagers.RegisterMethodAdd.UnPack(data.Array.Bytes, data.Offset, out _, out ushort RegID, out string nameMethod);
                            MyRPCAdressing.Add(RegID, nameMethod);
                            break;
                        case RegisterMethodType.Remove:
                            Packagers.RegisterMethodRemove.UnPack(data.Array.Bytes, data.Offset, out _, out ushort methodID);
                            MyRPCAdressing.Remove(methodID);
                            break;
                        case RegisterMethodType.SendList:

                            Packagers.RegisterMethodList.UnPack(data.Array.Bytes, data.Offset, out _, out ushort[] RegIDs, out string[] nameMethods);
                            for (int i = 0; i < RegIDs.Length; i++)
                            {
                                if (RegIDs.Length != nameMethods.Length)
                                    MyNetworkClient.Disconnect("bad package data (RegisterMethodType.SendList)");

                                MyRPCAdressing.Add(RegIDs[i], nameMethods[i]);
                            }

                            TaskUtilities.Try(() => WaitAcceptSendList.Cancel());
                            break;
                        default:
                            MyNetworkClient.Disconnect("bad package header (flags)");
                            break;
                    }
                    data.Array.Release();
                    break;
                case PacketType.RPC:
                    switch ((RPCType)packetHeader.Flags)
                    {
                        case RPCType.Simple:
                            {
                                Packagers.RPC.UnPack(data.Array.Bytes, data.Offset, out _, out ushort id, out long time);
                                data.Offset += Packagers.RPCSizeOf;

                                handle.Ping = new TimeSpan(MyTimerSync.SyncTicks - time);
                                var func = RPC.TryGetRegisteredMethod(id);

                                if (func != null)
                                {
                                    await func(handle, data, false, token).ConfigureAwait(false);
                                }
                            }
                            break;
                        case RPCType.NeedReturn:
                            {
                                Packagers.RPC.UnPack(data.Array.Bytes, data.Offset, out _, out ushort id, out long time);
                                data.Offset += Packagers.RPCSizeOf;

                                handle.Ping = new TimeSpan(MyTimerSync.SyncTicks - time);

                                var func = RPC.TryGetRegisteredMethod(id);

                                if (func != null)
                                {
                                    var outData = await func(handle, data, false, token).ConfigureAwait(false);
                                    if (token.IsCancellationRequested)
                                        return;
                                    Packagers.RPCAnswer.PackUP(outData.Bytes, 0, new PacketHeader(RPCType.ReturnAnswer), id, RPCReturnStatus.Good);
                                    await MyNetworkClient.SendAsync(outData, true, token).ConfigureAwait(false);
                                    outData.Release();
                                }
                                else
                                {
                                    var arraySend = await MyArrayBufferSend.AllocateArrayAsync(Packagers.RPCAnswerSizeOf, token).ConfigureAwait(false);
                                    if (token.IsCancellationRequested)
                                        return;
                                    Packagers.RPCAnswer.PackUP(arraySend.Bytes, 0, new PacketHeader(RPCType.ReturnAnswer), id, RPCReturnStatus.FunctionNotFound);
                                    await MyNetworkClient.SendAsync(arraySend, true, token).ConfigureAwait(false);
                                    arraySend.Release();
                                }
                            }
                            break;
                        case RPCType.ReturnAnswer:
                            {
                                Packagers.RPCAnswer.UnPack(data.Array.Bytes, data.Offset, out _, out ushort id, out RPCReturnStatus rrs);
                                data.Offset += Packagers.RPCAnswerSizeOf;

                                lock (RPCReturn)
                                {
                                    bool good = false;
                                    for (int i = 0; i < RPCReturn.Count; i++)
                                    {
                                        if (RPCReturn[i].ID == id)
                                        {
                                            RPCReturn[i].InputData = data;
                                            RPCReturn[i].RRS = rrs;
                                            RPCReturn[i].Semaphore.Release();
                                            good = true;
                                            break;
                                        }
                                    }
                                    if (!good)
                                    {
                                        MyNetworkClient.Disconnect("bad package");
                                    }
                                }
                            }
                            break;
                        default:
                            MyNetworkClient.Disconnect("bad package header (flags)");
                            break;
                    }
                    data.Array.Release();
                    break;
                default:
                    Console.WriteLine("Bad");
                    MyNetworkClient.Disconnect("bad package header (PacketType)");
                    data.Array.Release();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indicator">Cсылка на метод</param>
        /// <param name="invokeInfo">Информация о том как следует вызвать метод</param>
        /// <returns>*выполнилась ли функция (если false значит она не была найдена) *если ожидания возврата функции не было то вернёт true)</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> Invoke(Indicator indicator, RPCInvokeInfo invokeInfo)
        {
            var arraySend = await MyArrayBufferSend.AllocateArrayAsync(Packagers.RPCSizeOf, default).ConfigureAwait(false);

            ushort? rID = 0;

            lock (MyRPCAdressing)
            {
                rID = MyRPCAdressing.ReadresingID[indicator.ID];
                if (rID == null)
                {
                    if (MyRPCAdressing.DoReaddressing(indicator.ID, indicator.Name))
                    {
                        rID = MyRPCAdressing.ReadresingID[indicator.ID];
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            Packagers.RPC.PackUP(arraySend.Bytes, 0, new PacketHeader(RPCType.Simple), rID.Value, MyTimerSync.SyncTicks);
            Console.WriteLine(MyTimerSync.SyncTicks);
            await MyNetworkClient.SendAsync(arraySend, invokeInfo == RPCInvokeInfo.Guarantee || invokeInfo == RPCInvokeInfo.GuaranteeAndWaitReturn, default).ConfigureAwait(false);
            arraySend.Release();

            if (invokeInfo == RPCInvokeInfo.GuaranteeAndWaitReturn)
            {
                WaitHandle wait = new WaitHandle()
                {
                    ID = rID.Value,
                    Semaphore = new SemaphoreSlim(0, 1),
                };
                lock (RPCReturn)
                {
                    RPCReturn.Add(wait);
                }
                CancellationToken token = CancellationRun.Token;
                await wait.Semaphore.WaitAsync(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    throw new ClientDisconnectException();

                lock (RPCReturn)
                {
                    RPCReturn.Remove(wait);
                }

                if (wait.RRS == RPCReturnStatus.FunctionNotFound)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }


    internal class WaitHandle
    {
        public ushort ID;
        public SemaphoreSlim Semaphore;
        public Array2Offser InputData;
        public RPCReturnStatus RRS;
    }
}