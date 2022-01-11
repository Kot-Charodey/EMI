using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    using Network;
    using MyException;

    /// <summary>
    /// Сервер
    /// </summary>
    public class Server
    {
        /// <summary>
        /// RPC
        /// </summary>
        public RPC RPC { get; private set; } = new RPC();
        /// <summary>
        /// Запущен ли сервер
        /// </summary>
        public bool IsRun { get; private set; }

        private CancellationTokenSource CancellationTokenSource;
        private List<Client> Clients;
        private readonly INetworkService Service;
        private readonly INetworkServer LowServer;
        /// <summary>
        /// Вызывается раз в секунду для проверки пинга
        /// </summary>
        internal event RPCfuncOut<Task> PingSend;

        /// <summary>
        /// Создаёт новую копию сервера
        /// </summary>
        /// <param name="service">интерфейс подключения</param>
        public Server(INetworkService service)
        {
            Service = service;
            LowServer = Service.GetNewServer();
        }

        /// <summary>
        /// Запускает сервер
        /// </summary>
        /// <param name="address">локальный адрес прослушивания</param>
        /// <exception cref="AlreadyException">сервер уже запущен</exception>
        public void Start(string address)
        {
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
        }

        private void PingProcessStart()
        {
            Task.Run(async () =>
            {
                var token = CancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (PingSend != null)
                        await PingSend().ConfigureAwait(false);
                }
            });
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
            var LowClient = await LowServer.AcceptClient().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Client client = null;

            CancellationTokenSource cts = new CancellationTokenSource();
            token.Register(() => cts.Cancel());
            await TaskUtilities.InvokeAsync(() =>
            {
                client = Client.CreateClinetServerSide(LowClient, RPC, this);
            }, cts).ConfigureAwait(false);

            var list = Clients;
            client.Disconnected += (_) =>
            {
                lock (list)
                {
                    list.Remove(client);
                }
            };
            return client;
        }
    }
}