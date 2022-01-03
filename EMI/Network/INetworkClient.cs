using System.Threading;
using System.Threading.Tasks;
using System;

namespace EMI.Network
{
    using EMI.ProBuffer;
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
        /// Конвеер для выдачи массивов "ИЗБЕГАТЬ создания объектов"
        /// Устанавливается библиотекой при инициализации
        /// </summary>
        ProArrayBuffer ProArrayBuffer { set; }
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
        /// <param name="token">токен отмены задачи</param>
        Task<bool> Сonnect(string address, CancellationToken token);
        /// <summary>
        /// Отключиться от сервера
        /// </summary>
        void Disconnect(string user_error);
        /// <summary>
        /// Отправить данные асинхронно
        /// </summary>
        /// <param name="array">массив данных</param>
        /// <param name="guaranteed">необходимо гарантированно доставить пакет (если false для оптимизации следует по возможности использовать негарантированную доставку, тоесть пакет может быть потерян)</param>
        /// <param name="token">токен отмены задачи</param>
        Task SendAsync(IReleasableArray array, bool guaranteed, CancellationToken token);
        /// <summary>
        /// Отправить данные
        /// </summary>
        /// <param name="array">массив данных</param>
        /// <param name="guaranteed">необходимо гарантированно доставить пакет (если false для оптимизации следует по возможности использовать негарантированную доставку, тоесть пакет может быть потерян)</param>
        void Send(IReleasableArray array, bool guaranteed);
        /// <summary>
        /// Считывает пришедшие данные
        /// </summary>
        /// <param name="token">токен отмены задачи</param>
        /// <returns>массив и смещение от куда можно начинать считывать данные</returns>
        Task<Array2Offser> Accept(CancellationToken token);
    }
}
