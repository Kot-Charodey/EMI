using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    using Network;
    using ProBuffer;

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


        private ProArrayBuffer MyArrayBuffer;
        /// <summary>
        /// Интерфейс отправки/считывания датаграмм
        /// </summary>
        private INetworkClient MyNetworkClient;
        /// <summary>
        /// Таймер времени - позволяет измерять пинг
        /// </summary>
        private TimerSync MyTimerSync;
        /// <summary>
        /// Вызывается если неуспешный Connect или произошло отключение
        /// </summary>
        public event INetworkClientDisconnected Disconnected;

        internal InputSerialWaiter<byte[]> TimerSyncInputTick;
        internal InputSerialWaiter<byte[]> TimerSyncInputInteg;

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

            MyArrayBuffer = new ProArrayBuffer(100, 1024 * 10);//~1 MB

            TimerSyncInputTick = new InputSerialWaiter<byte[]>();
            TimerSyncInputInteg = new InputSerialWaiter<byte[]>();

            MyNetworkClient.Disconnected += LowDisconnect;
            MyNetworkClient.ProArrayBuffer = MyArrayBuffer;
        }

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="address">адрес сервера</param>
        /// <param name="token">токен отмены задачи</param>
        /// <returns>было ли произведено подключение</returns>
        public async Task<bool> Connect(string address,CancellationToken token)
        {
            var status = await MyNetworkClient.Сonnect(address,token).ConfigureAwait(false);

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
            }
        }

        private async Task ProccesAccept(CancellationToken token)
        {
            IReleasableArray buffer = await MyNetworkClient.Accept(token).ConfigureAwait(false);
            
        }
    }
}
