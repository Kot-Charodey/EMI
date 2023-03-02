using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    using EMI.LogMessages;
    using MyException;
    using Network;
    using NGC;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Клиент EMI
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Отвечает за регистрирование удалённых процедур для последующего вызова [локальный - вызов будет произведён только у этого клиента]
        /// </summary>
        public readonly RPC LocalRPC = new RPC();
        /// <summary>
        /// Отвечает за регистрирование удалённых процедур для последующего вызова [глобальный]
        /// </summary>
        public RPC RPC { get; private set; }
        /// <summary>
        /// Подключён ли клиент
        /// </summary>
        public bool IsConnect => MyNetworkClient.IsConnect;
        /// <summary>
        /// Этот клиент на стороне сервера?
        /// </summary>
        public bool IsServerSide => Server != null;
        /// <summary>
        /// Ping
        /// </summary>
        public TimeSpan Ping = new TimeSpan(0);
        /// <summary>
        /// Время после которого будет произведено отключение
        /// </summary>
        public TimeSpan PingTimeout = new TimeSpan(0, 1, 0);
        /// <summary>
        /// Вызывается если произошло отключение
        /// </summary>
        public event INetworkClientDisconnected Disconnected;
        /// <summary>
        /// Максимальный размер пакета который может отправить удалённый пользователь за один раз (если размер будет превышен - клиент будет отключен)
        /// </summary>
        public int MaxPacketAcceptSize = 1024 * 1024 * 10;
        /// <summary>
        /// Интерфейс отправки/считывания датаграмм
        /// </summary>
        internal INetworkClient MyNetworkClient;
        /// <summary>
        /// Когда приходил прошлый запрос о пинге (для time out)
        /// </summary>
        private DateTime LastPing;
        /// <summary>
        /// Токен вызывающийся при отмене подключения (все операции должны быть отменены)
        /// </summary>
        internal CancellationTokenSource CancellationRun = new CancellationTokenSource();
        internal InputStackBuffer InputStack;
        private readonly static TaskFactory TaskLongFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
        /// <summary>
        /// Массив запросов на ожидание ответа (возврат значения)
        /// </summary>
        internal Dictionary<int, RCWaitHandle> RPCReturn;
        /// <summary>
        /// Ссылка на сервер (если клиент серверный)
        /// </summary>
        private readonly Server Server;
        /// <summary>
        /// Логи сервера и клиентов
        /// </summary>
        public readonly DebugLog.Logger Logger;
        /// <summary>
        ///  Возвращает адресс удалённого связанного клиента
        /// </summary>
        public string RemoteAddress
        {
            get
            {
                if (IsConnect)
                {
                    return MyNetworkClient.GetRemoteClientAddress();
                }
                else
                {
                    return "none";
                }
            }
        }

        /// <summary>
        /// Инициализирует клиента но не подключает к серверу
        /// </summary>
        /// <param name="network">интерфейс подключения</param>
        public Client(INetworkService network)
        {
            Logger = new DebugLog.Logger();
            MyNetworkClient = network.GetNewClient();
            RPC = LocalRPC;
            Init();

            Logger.Log(this, DebugLog.LogType.Message, Messages.Log(1, network));
        }

        /// <summary>
        /// Для сосздания клиента на стороне сервера
        /// </summary>
        /// <param name="network"></param>
        /// <param name="rpc"></param>
        /// <param name="server"></param>
        internal Client(INetworkClient network, RPC rpc, Server server)
        {
            Logger = server.Logger;
            MyNetworkClient = network;
            RPC = rpc;
            Server = server;
            Init();
            RunProcces();

            Logger.Log(this, DebugLog.LogType.Message, Messages.Log(2, network));
        }

        /// <summary>
        /// Для инициализации клиента
        /// </summary>
        private void Init()
        {
            InputStack = new InputStackBuffer(64, 134217728);
            RPCReturn = new Dictionary<int, RCWaitHandle>();
            MyNetworkClient.Disconnected += LowDisconnect;
        }

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="address">адрес сервера</param>
        /// <param name="token">токен отмены задачи</param>
        /// <returns>было ли произведено подключение</returns>
        public async Task<bool> Connect(string address, CancellationToken token)
        {
            Logger.Log(this, DebugLog.LogType.Message, Messages.Log(3, IsConnect, IsServerSide));

            if (IsConnect)
            {
                string msg = Messages.Log(8);
                Logger.Log(this, DebugLog.LogType.CriticalError, msg);
                throw new AlreadyException(msg);
            }
            if (IsServerSide)
            {
                string msg = Messages.Log(9);
                Logger.Log(this, DebugLog.LogType.CriticalError, msg);
                throw new Exception(msg);
            }

            CancellationRun = new CancellationTokenSource();

            var status = await MyNetworkClient.Сonnect(address, token).ConfigureAwait(false);

            Logger.Log(this, DebugLog.LogType.Message, Messages.Log(4, status));

            if (token.IsCancellationRequested && status == true)
            {
                string msg = Messages.Log(5);
                Logger.Log(this, DebugLog.LogType.Message, msg);
                MyNetworkClient.Disconnect(msg);
                DoCancellationRun();

                Logger.Log(this, DebugLog.LogType.Message, Messages.Log(7));
                return false;
            }

            if (status == true)
            {
                RunProcces();
                Logger.Log(this, DebugLog.LogType.Message, Messages.Log(6));
                return true;
            }
            else
            {
                Logger.Log(this, DebugLog.LogType.Message, Messages.Log(7));
                return false;
            }
        }

        /// <summary>
        /// Закрывает соединение
        /// </summary>
        /// <param name="user_error">что сообщить клиенту при отключении</param>
        public void Disconnect(string user_error = "unknown")
        {
            Logger.Log(this, DebugLog.LogType.Message, Messages.Log(10, user_error));

            if (!IsConnect)
            {
                string msg = Messages.Log(11);
                Logger.Log(this, DebugLog.LogType.Error, msg);
                throw new AlreadyException(msg);
            }
            MyNetworkClient.Disconnect(user_error);
            //DoCancellationRun();
        }

        /// <summary>
        /// Вызвать при внутренем отключение
        /// </summary>
        /// <param name="error"></param>
        private void LowDisconnect(string error)
        {
            Logger.Log(this, DebugLog.LogType.CriticalError, Messages.Log(12, error));
            Disconnected?.Invoke(error);
            DoCancellationRun();
        }

        /// <summary>
        /// Отменить работу клиента
        /// </summary>
        private void DoCancellationRun()
        {
            try
            {
                CancellationRun.Cancel();
                CancellationRun.Dispose();
                CancellationRun = new CancellationTokenSource();
                //var cr = CancellationRun;
                //CancellationRun = null;
                //cr.Cancel();
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000).ConfigureAwait(false);
                        //cr.Dispose();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(this, DebugLog.LogType.Error, Messages.Log(13, e.ToString()));
                    }
                });
            }
            finally
            {
                //сброс компонентов для реиспользования клиента
                if (!IsServerSide)
                {
                    RPCReturn.Clear();
                }
            }
        }

        /// <summary>
        /// Запускает все необходимые потоки
        /// </summary>
        private void RunProcces()
        {
            TaskLongFactory.StartNew(() =>
            {
                _ = RunProcessAccept(CancellationRun.Token);
            });
            RunProccesPing(CancellationRun.Token);
        }

        /// <summary>
        /// Отвечает за отправку пинга + за отключение по ping timeout
        /// </summary>
        /// <param name="token">токен отмены</param>
        private void RunProccesPing(CancellationToken token)
        {
            TaskLongFactory.StartNew(async () =>
            {
                Logger.Log(this, DebugLog.LogType.Message, Messages.Log(25));
                LastPing = TickTime.Now;
                const int size = DPack.sizeof_DPing + 1;
                var array = new EasyArray(size);
                array.Bytes[0] = (byte)PacketType.Ping_Send;

                async Task pingTask()
                {
                    try
                    {
                        if (TickTime.Now - LastPing > PingTimeout)
                        {
                            string msg = Messages.Log(14, (TickTime.Now - LastPing).TotalMilliseconds);
                            Logger.Log(this, DebugLog.LogType.CriticalError, msg);
                            MyNetworkClient.Disconnect(msg);

                            if (IsServerSide)
                                lock (Server)
                                    Server.PingSend -= pingTask;
                            return;
                        }
                        else
                        {
                            DPack.DPing.PackUP(array.Bytes, 1, TickTime.Now);
                            await MyNetworkClient.Send(array, false, token).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        string msg = Messages.Log(15, e.ToString());
                        Logger.Log(this, DebugLog.LogType.CriticalError, msg);

                        MyNetworkClient.Disconnect(msg);
                        if (IsServerSide)
                            lock (Server)
                                Server.PingSend -= pingTask;
                        return;
                    }
                }

                if (IsServerSide)
                {
                    lock (Server)
                        Server.PingSend += pingTask;
                }
                else
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        await pingTask().ConfigureAwait(false);
                    }
                    Logger.Log(this, DebugLog.LogType.Message, Messages.Log(16));
                }
            });
        }

        /// <summary>
        /// Основной процесс обработки входящих пакетов
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task RunProcessAccept(CancellationToken token)
        {
            Logger.Log(this, DebugLog.LogType.Message, Messages.Log(24));

            void process()
            {
                try
                {
                    ProccesAccept(token);
                }
                catch (Exception e)
                {
                    string msg = Messages.Log(17, e.ToString());
                    Logger.Log(this, DebugLog.LogType.CriticalError, msg);
                    MyNetworkClient.Disconnect(msg);
                }
            }

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var packet = await MyNetworkClient.AcceptPacket(MaxPacketAcceptSize, token);
                    if (packet.IsEmpty())
                        break;
                    
                    await InputStack.Push(packet, token).ConfigureAwait(false);
                    _ = Task.Factory.StartNew(process);
                }
                Logger.Log(this, DebugLog.LogType.Message, Messages.Log(18));
            }
            catch (Exception e)
            {
                Logger.Log(this, DebugLog.LogType.Message, Messages.Log(17, e.ToString()));
            }
        }

        /// <summary>
        /// Процесс обработки пакета
        /// </summary>
        /// <param name="token">токен отмены</param>
        /// <returns></returns>
        private async void ProccesAccept(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            using (var ArrayHandle = await InputStack.Pop(token))
            {
                if (token.IsCancellationRequested || ArrayHandle.Buffer.IsEmpty())
                    return;
                var array = ArrayHandle.Buffer;
                PacketType packetType = (PacketType)array.Bytes[array.Offset];
                //Debug.WriteLine("accept => " + packetType);
                array.Offset += 1;
                switch (packetType)
                {
                    case PacketType.Ping_Send:
                        array.Bytes[array.Offset - 1] = (byte)PacketType.Ping_Receive;
                        await MyNetworkClient.Send(array, false, token).ConfigureAwait(false);
                        break;
                    case PacketType.Ping_Receive:
                        DPack.DPing.UnPack(array.Bytes, array.Offset, out var time);
                        if (LastPing < time)
                            Ping = TickTime.Now - time;
                        LastPing = TickTime.Now;
                        break;
                    case PacketType.RPC_Simple:
                        {
                            _ = RPCRun(false, array, token).ConfigureAwait(false);
                        }
                        break;
                    case PacketType.RPC_Return:
                        {
                            _ = RPCRun(true, array, token).ConfigureAwait(false);
                        }
                        break;
                    case PacketType.RPC_Returned:
                        {
                            //TODO - тут какета хуйня проиходит
                            DPack.DRPC.UnPack(array.Bytes, array.Offset, out var id);
                            array.Offset += sizeof(int);

                            RCWaitHandle handle;

                            CancellationTokenSource source = new CancellationTokenSource(new TimeSpan(0, 5, 0));

                            while (!RPCReturn.TryGetValue(id, out handle))
                            {
                                if (source.IsCancellationRequested)
                                {
                                    string err_str = Messages.Log(19);
                                    Logger.Log(this, DebugLog.LogType.CriticalError, err_str);
                                    MyNetworkClient.Disconnect(err_str);
                                    return;
                                }
                                await Task.Yield();
                            }

                            source.Dispose();

                            lock (RPCReturn)
                            {
                                RPCReturn.Remove(id);
                            }
                            handle.Indicator.UnPack(array);
                            handle.Semaphore.Release();
                        }
                        break;
                    case PacketType.RPC_Forwarding:
                        if (!IsServerSide)
                        {
                            Logger.Log(this, DebugLog.LogType.CriticalError, Messages.Log(20));
                            MyNetworkClient.Disconnect(Messages.Log(20));
                        }
                        else
                        {
                            //*при RPC_Forwarding отправляется сообщение на сервер содержащие флаг - гарантированное ли было сообщение, а после она рассылается как обычное сообщение*//
                            DPack.DForwarding.UnPack(array.Bytes, array.Offset, out var guarant, out var id);
                            array.Offset++;
                            var forwardingInfo = RPC.TryGetRegisteredForwarding(id);

                            if (forwardingInfo == null)
                            {
                                forwardingInfo = LocalRPC.TryGetRegisteredForwarding(id);
                            }

                            if (forwardingInfo != null)
                            {
                                var clients = forwardingInfo(this);
                                using (var arraySend = new NGCArray(array.Length - 1))
                                {
                                    arraySend.Bytes[0] = (byte)PacketType.RPC_Simple;
                                    Buffer.BlockCopy(array.Bytes, array.Offset, arraySend.Bytes, 1, arraySend.Length - 1);

                                    for (int i = 0; i < clients.Length; i++)
                                    {
                                        if (clients[i] != null && clients[i].IsConnect)
                                        {
                                            await clients[i].MyNetworkClient.Send(arraySend, guarant, token).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Logger.Log(this, DebugLog.LogType.Waring, Messages.Log(21));
                            }
                        }
                        break;
                    default:
                        string msg = Messages.Log(22);
                        Logger.Log(this, DebugLog.LogType.CriticalError, msg);
                        MyNetworkClient.Disconnect(msg);
                        break;
                }
            }
        }

        /// <summary>
        /// Вызов метода
        /// </summary>
        /// <param name="needReturn">нужно ли отправить результат</param>
        /// <param name="array">массив данных для отправки</param>
        /// <param name="token">токен отмены</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task RPCRun(bool needReturn, INGCArray array, CancellationToken token)
        {
            DPack.DRPC.UnPack(array.Bytes, array.Offset, out var id);
            array.Offset += sizeof(int);
            var funcs = RPC.TryGetRegisteredMethod(id);

            if (funcs == null)
            {
                funcs = LocalRPC.TryGetRegisteredMethod(id);
            }

            if (funcs != null)
            {
                if (needReturn)
                {
                    var @return = funcs.Invoke(array);
                    const int bsize = DPack.sizeof_DRPC + 1;
                    int size = bsize;
                    if (@return != null)
                        size += @return.PackSize;

                    INGCArray sendArray = new NGCArray(size);
                    try
                    {
                        sendArray.Bytes[0] = (byte)PacketType.RPC_Returned;
                        DPack.DRPC.PackUP(sendArray.Bytes, 1, id);
                        if (@return != null)
                        {
                            sendArray.Offset += bsize;
                            @return.PackUp(sendArray);
                        }
                        await MyNetworkClient.Send(sendArray, true, token);
                    }
                    finally
                    {
                        sendArray.Dispose();
                    }
                }
                else
                {
                    funcs.Invoke(array);
                }
            }
            else
            {
                Logger.Log(this, DebugLog.LogType.Waring, Messages.Log(23, id));
            }
        }
    }
}