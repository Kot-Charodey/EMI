using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    using Network;

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
        private INetworkClient MyNetworkClient;

        /// <summary>
        /// Вызывается если неуспешный Connect или произошло отключение
        /// </summary>
        public event INetworkClientDisconnected Disconnected;

        /// <summary>
        /// Инициализирует клиента но не подключает к серверу
        /// </summary>
        /// <param name="network">интерфейс подключения</param>
        public Client(INetworkClient network)
        {
            MyNetworkClient = network;
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
        /// <param name="rpc"></param>
        /// <returns></returns>
        internal static Client CreateClinetServerSide(INetworkClient network,RPC rpc)
        {
            Client client = new Client()
            {
                MyNetworkClient = network,
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
            MyNetworkClient.Disconnected += LowDisconnect;
        }

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="address">адрес сервера</param>
        /// <returns>было ли произведено подключение</returns>
        public async Task<bool> Connect(string address)
        {
            var status = await MyNetworkClient.Сonnect(address).ConfigureAwait(false);
            if (status == true)
            {
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
            //TODO отправить user_error
            MyNetworkClient.Disconnect();
        }

        /// <summary>
        /// Вызвать при внутренем отключение
        /// </summary>
        /// <param name="error"></param>
        private void LowDisconnect(string error)
        {
            Disconnected?.Invoke(error);
        }
    }
}
