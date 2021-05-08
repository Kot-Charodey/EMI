using System;

namespace EMI
{
    using Lower.Accepter;

    /// <summary>
    /// Сервер EMI
    /// </summary>
    public class Server
    {
        private MultiAccepter MultiAccepter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public Server(ushort port)
        {
            MultiAccepter = new MultiAccepter(port);
        }

        /// <summary>
        /// Запускает сервер и начинает принимать новых пользователей
        /// </summary>
        /// <param name="accepterEvent">Событие подключения нового пользователя [создаёт новый поток]</param>
        public void Start(Action<Client> accepterEvent)
        {
            MultiAccepter.StartProcessReceive(accepterEvent);
        }

        /// <summary>
        /// Завершает работу сервера и отсоединяет всех клиентов [для повторного запуска следует создать новую копию класса]
        /// </summary>
        public void Stop()
        {
            MultiAccepter.Stop();
            MultiAccepter = null;
        }
    }
}
