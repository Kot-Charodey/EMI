using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
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
        public bool IsServerSide { get; private set; } = false;
        /// <summary>
        /// Ping
        /// </summary>
        public TimeSpan Ping = new TimeSpan(0);
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

        /// <summary>
        /// Интерфейс отправки/считывания датаграмм
        /// </summary>
        internal INetworkClient MyNetworkClient;
        /// <summary>
        /// Когда приходил прошлый запрос о пинге (для time out)
        /// </summary>
        private DateTime LastPing;
        internal CancellationTokenSource CancellationRun = new CancellationTokenSource();
        internal Dictionary<int, RCWaitHandle> RPCReturn;
        private Server Server;


        /// <summary>
        /// Инициализирует клиента но не подключает к серверу
        /// </summary>
        /// <param name="network">интерфейс подключения</param>
        public Client(INetworkService network)
        {
            MyNetworkClient = network.GetNewClient();
            RPC = LocalRPC;
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
        /// <param name="rpc"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        internal static Client CreateClinetServerSide(INetworkClient network, RPC rpc, Server server)
        {
            Client client = new Client()
            {
                MyNetworkClient = network,
                RPC = rpc,
                Server = server
            };
            client.IsServerSide = true;
            client.Init();
            client.RunProcces();

            return client;
        }

        /// <summary>
        /// Для инициализации клиента
        /// </summary>
        private void Init()
        {
            MyArrayBuffer = new ProArrayBuffer(200, 1024);
            MyArrayBufferSend = new ProArrayBuffer(200, 1024);
            RPCReturn = new Dictionary<int, RCWaitHandle>();

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

            CancellationRun = new CancellationTokenSource();

            var status = await MyNetworkClient.Сonnect(address, token).ConfigureAwait(false);

            if (status == true)
            {
                RunProcces();
                token.ThrowIfCancellationRequested();

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
            try
            {
                CancellationRun.Cancel();
            }
            catch { }

            //сброс компонентов для реиспользования клиента
            if (!IsServerSide)
            {
                MyArrayBuffer.Reinit();
                MyArrayBufferSend.Reinit();
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
                const int size = DPack.sizeof_DPing + 1;

                async Task pingTask()
                {
                    try
                    {
                        //Console.WriteLine(DateTime.UtcNow - LastPing);
                        if (DateTime.UtcNow - LastPing > PingTimeout)
                        {
                            MyNetworkClient.Disconnect($"Timeout (Timeout = {(DateTime.UtcNow - LastPing).TotalMilliseconds} ms)");
                            if (IsServerSide)
                                lock (Server)
                                    Server.PingSend -= pingTask;
                            return;
                        }
                        else
                        {
                            IReleasableArray array = await MyArrayBufferSend.AllocateArrayAsync(size, token).ConfigureAwait(false);
                            array.Bytes[0] = (byte)PacketType.Ping_Send;
                            DPack.DPing.PackUP(array.Bytes, 1, DateTime.UtcNow);
                            await MyNetworkClient.SendAsync(array, false, token).ConfigureAwait(false);
                            array.Release();
                        }
                    }
                    catch
                    {
                        MyNetworkClient.Disconnect($"Connection close");
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
            //SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
            for (int i = 0; i < 20; i++)
            {
                factory.StartNew(async () =>
                {
                    while (true)
                    {
                        //await semaphore.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            await ProccesAccept(token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException e)
                        {
                            throw e;
                        }catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        //semaphore.Release();
                    }
                });
            }
        }

        private async Task ProccesAccept(CancellationToken token)
        {
            var array = await MyNetworkClient.AcceptAsync(token).ConfigureAwait(false);
            PacketType packetType = (PacketType)array.Bytes[array.Offset];
            array.Offset += 1;
            switch (packetType)
            {
                case PacketType.Ping_Send:
                    array.Bytes[array.Offset - 1] = (byte)PacketType.Ping_Receive;
                    await MyNetworkClient.SendAsync(array, false, token).ConfigureAwait(false);
                    array.Release();
                    break;
                case PacketType.Ping_Receive:
                    DPack.DPing.UnPack(array.Bytes, array.Offset, out var time);
                    if (LastPing < time)
                        Ping = DateTime.UtcNow - time;
                    LastPing = DateTime.UtcNow;
                    array.Release();
                    break;
                case PacketType.RPC_Simple:
                    {
                        await RPCRun(false, array, token);
                    }
                    break;
                case PacketType.RPC_Return:
                    {
                        await RPCRun(true, array, token);
                    }
                    break;
                case PacketType.RPC_Returned:
                    {
                        DPack.DRPC.UnPack(array.Bytes, array.Offset, out var id);
                        array.Offset += sizeof(int);

                        RCWaitHandle handle = null;

                        CancellationTokenSource source = new CancellationTokenSource(new TimeSpan(0, 5, 0));

                        while (!RPCReturn.TryGetValue(id, out handle))
                        {
                            if (source.IsCancellationRequested)
                            {
                                MyNetworkClient.Disconnect("Bad packet header data");
                                return;
                            }
                            await Task.Yield();
                        }

                        source.Dispose();

                        lock (RPCReturn)
                        {
                            RPCReturn.Remove(id);
                        }
                        handle.Array = array;
                        handle.Semaphore.Release();
                    }
                    break;
                case PacketType.RPC_Forwarding:
                    if (!IsServerSide)
                    {
                        MyNetworkClient.Disconnect("Bad packet type (forwarding not supported on clients)");
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
                            var arraySend = await MyArrayBufferSend.AllocateArrayAsync(array.Length - 1, token).ConfigureAwait(false);

                            arraySend.Bytes[0] = (byte)PacketType.RPC_Simple;
                            Buffer.BlockCopy(array.Bytes, array.Offset, arraySend.Bytes, 1, arraySend.Length - 1);

                            for (int i = 0; i < clients.Length; i++)
                            {
                                if (clients[i] != null && clients[i].IsConnect)
                                {
                                    try
                                    {
                                        await clients[i].MyNetworkClient.SendAsync(arraySend, guarant, token).ConfigureAwait(false);
                                    }
                                    catch { }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("EMI RC => Not found forwarding!");
                        }

                        array.Release();
                    }
                    break;
                default:
                    MyNetworkClient.Disconnect("Bad packet type");
                    break;
            }
        }

        private async Task RPCRun(bool needReturn, IReleasableArray array, CancellationToken token)
        {
            DPack.DRPC.UnPack(array.Bytes, array.Offset, out var id);
            array.Offset += sizeof(int);
            var funcs = RPC.TryGetRegisteredMethod(id);

            if(funcs==null)
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

                    var sendArray = await MyArrayBufferSend.AllocateArrayAsync(size, token);
                    sendArray.Bytes[0] = (byte)PacketType.RPC_Returned;
                    DPack.DRPC.PackUP(sendArray.Bytes, 1, id);
                    if (@return != null)
                    {
                        sendArray.Offset += bsize;
                        @return.PackUp(sendArray);
                    }
                    await MyNetworkClient.SendAsync(sendArray, true, token);
                    sendArray.Release();
                }
                else
                {
                    funcs.Invoke(array);
                }
            }
            else
            {
                Console.WriteLine("EMI RC => Not found method!");
            }

            array.Release();
        }
    }
}