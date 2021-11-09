﻿using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MSTests")]
[assembly: InternalsVisibleTo("DebTest")]
namespace EMI
{
    using Lower.Accepter;

    /// <summary>
    /// Сервер EMI
    /// </summary>
    public class Server
    {
        private MultiAccepter MultiAccepter;
        private readonly ushort Port;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public Server(ushort port)
        {
            Port = port;
        }

        /// <summary>
        /// Запускает сервер и начинает принимать новых пользователей
        /// </summary>
        /// <param name="accepterEvent">Событие подключения нового пользователя [создаёт новый поток]</param>
        public void Start(Action<Client> accepterEvent)
        {
            MultiAccepter = new MultiAccepter(Port,this);
            MultiAccepter.StartProcessReceive(accepterEvent);
        }

        /// <summary>
        /// Завершает работу сервера и отсоединяет всех клиентов
        /// </summary>
        public void Stop()
        {
            MultiAccepter.Stop();
            MultiAccepter = null;
        }
    }
}
