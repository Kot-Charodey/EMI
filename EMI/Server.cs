using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    using MyException;
    using Network;

    /// <summary>
    /// Сервер
    /// </summary>
    public class Server
    {
        /// <summary>
        /// Отвечает за регистрирование удалённых процедур для последующего вызова
        /// </summary>
        public RPC RPC { get; private set; } = new RPC();
        /// <summary>
        /// Запущен ли сервер
        /// </summary>
        public bool IsRun { get; private set; }

        private CancellationTokenSource CancellationTokenSource;
        private List<Client> Clients;
        /// <summary>
        /// Логи сервера и клиентов
        /// </summary>
        public readonly DebugLog.Logger Logger = new DebugLog.Logger();
        private readonly INetworkService Service;
        private readonly INetworkServer LowServer;


#if DEBUG
        private Client[] GetClients()
        {
            if (Clients == null)
                return new Client[0];
            else
                return Clients.ToArray();
        }
#endif

        /// <summary>
        /// Список клиентов подключеных к серверу
        /// </summary>
        public Client[] ServerClients =>
#if DEBUG
            GetClients();
#else
            throw new NotSupportedException();
#endif
        /// <summary>
        /// Имя сервиса по которому осуществляется низкоуровневый обмен сообщениями
        /// </summary>
        public string ServiceName =>
#if DEBUG
            Service.ToString();
#else
            throw new NotSupportedException();
#endif
        private string _address = "";

        /// <summary>
        /// Адресс по которому сервер слушает подключения
        /// </summary>
        public string GetAddress =>
#if DEBUG
        _address;
#else
        throw new NotSupportedException();
#endif

        /// <summary>
        /// Происходит когда клиент подключился
        /// </summary>
        public event Action<Client> OnClientConnect;
        /// <summary>
        /// Происходит когда клиент отключается
        /// </summary>
        public event Action<Client> OnClientDisconnect;

        /// <summary>
        /// Вызывается раз в секунду для проверки пинга
        /// </summary>
        internal event RPCfuncOut<Task> PingSend;

        /// <summary>
        /// Создаёт новый сервер
        /// </summary>
        /// <param name="service">интерфейс подключения</param>
        public Server(INetworkService service)
        {
            Service = service;
            LowServer = Service.GetNewServer();
            //TODO
            //Logger.Log(DebugLog.LogType.Message, $"Server => init (service: {service})");
        }

        /// <summary>
        /// Запускает сервер
        /// </summary>
        /// <param name="address">локальный адрес прослушивания</param>
        /// <exception cref="AlreadyException">сервер уже запущен</exception>
        public void Start(string address)
        {
            _address = address;
            lock (this)
            {
                if (IsRun)
                    throw new AlreadyException();
                PingSend = null;
                IsRun = true;
                Clients = new List<Client>();
                CancellationTokenSource = new CancellationTokenSource();

                LowServer.StartServer(address);
                PingProcessStart();
            }
            //TODO
            //Logger.Log(DebugLog.LogType.Message, "Server => started");
        }

        /// <summary>
        /// Останавливает сервер
        /// </summary>
        /// <exception cref="AlreadyException">Сервер уже остановлен</exception>
        public void Stop()
        {
            lock (this)
            {
                if (!IsRun)
                    throw new AlreadyException();
                IsRun = false;
                CancellationTokenSource.Cancel();
                lock (Clients)
                {
                    for (int i = 0; i < Clients.Count; i++)
                    {
                        try { Clients[i].Disconnect("Server is closed"); } catch { }
                    }
                }
            }
            //TODO
            //Logger.Log(DebugLog.LogType.Message, "Server => stoped");
        }

        private void PingProcessStart()
        {
            Task.Factory.StartNew(async () =>
            {
                var token = CancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (PingSend != null)
                        await PingSend().ConfigureAwait(false);
                }
                //TODO
                //Logger.Log(DebugLog.LogType.Message, "Server ping process => stoped");
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Ожидает подключение игрока
        /// </summary>
        /// <returns>подключённый клиент или null если операция отменена</returns>
        /// <exception cref="Exception">Сервер не был запущен</exception>
        public async Task<Client> Accept()
        {
            CancellationToken token;
            lock (this)
            {
                if (!IsRun)
                {
                    throw new Exception("Server is not running!");
                }
                token = CancellationTokenSource.Token;
            }
            var LowClient = await LowServer.AcceptClient(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Client client = null;

            CancellationTokenSource cts = new CancellationTokenSource();
            token.Register(() => cts.Cancel());
            await TaskUtilities.InvokeAsync(() =>
            {
                client = new Client(LowClient, RPC, this);
            }, cts).ConfigureAwait(false);

            var list = Clients;
            lock (list)
            {
                list.Add(client);
#if DEBUG
                OnClientConnect?.Invoke(client);
#endif
            }

            client.Disconnected += (_) =>
            {
                lock (list)
                {
                    list.Remove(client);
#if DEBUG
                    OnClientDisconnect?.Invoke(client);
#endif
                }
            };
            return client;
        }
    }
}