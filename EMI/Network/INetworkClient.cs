using System.Threading.Tasks;
using System;

namespace EMI.Network
{
    /* Рекомендации по реализации интерфейса
    * address для сетевых подключений в виде текста localhost:port
    * address для других типов интерфейсов может быть описан свой
    * при неверном address выдовать исключение с пояснительной ошибкой
    * следует использовать многопоточность/асинхронность
    * не асинхронные методы не могут блокировать поток
    * данный интерфейс является узким местом и поэтому должн максимально оптимизированн (ИЗБЕГАТЬ создания объектов)
    * event Disconnected должен вызываться даже при выполнении метода Сonnect и возвращать причину неуспешного подключения
    * при использовании сетевых сокетов и подобных проверять от того ли адреса приходят данные (для функции Accept) - посторонние данные отбрасывать
    * Disconnect по возможности должен собеседнику сообщать об отключении
    */
    /// <summary>
    /// Делегат для INetworkClient - вызывается когда клиент был отключен
    /// </summary>
    /// <param name="error">код/текст ошибки</param>
    public delegate void INetworkClientDisconnected(string error);

    /// <summary>
    /// Интерфейс для передачи данных между клиентом и сервером
    /// </summary>
    public interface INetworkClient
    {
        /// <summary>
        /// Подключён ли клиент
        /// </summary>
        bool IsConnect { get; }
        /// <summary>
        /// Аддрес клиента
        /// </summary>
        string Address { get; }
        /// <summary>
        /// Время которое не должен отвечать клиент до закрытия соединения
        /// </summary>
        TimeSpan TimeOutToDisconnect { get; set; }
        /// <summary>
        /// Вызывается при отключении клиента от сервера (вызывается если произойдёт сбой при вызове Сonnect)
        /// </summary>
        event INetworkClientDisconnected Disconnected;
        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="address">адрес сервера</param>
        /// <returns>было ли произведено подключение</returns>
        Task<bool> Сonnect(string address);
        /// <summary>
        /// Отключиться от сервера
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Отправить данные
        /// </summary>
        /// <param name="data">массив данных</param>
        /// <param name="length">сколько байт из массива будет отправленно</param>
        /// <param name="guaranteed">необходимо гарантированно доставить пакет (если false для оптимизации следует по возможности использовать негарантированную доставку, тоесть пакет может быть потерян)</param>
        void Send(byte[] data, int length, bool guaranteed);
        /// <summary>
        /// Считывает пришедшие данные
        /// </summary>
        /// <returns></returns>
        Task<byte[]> Accept();
    }
}
