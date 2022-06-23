using System.Threading;
using System.Threading.Tasks;
using System;

namespace EMI.Network
{
    using NGC;
    /* Рекомендации по реализации интерфейса
    * address для сетевых подключений в виде текста localhost#port
    * address для других типов интерфейсов может быть описан свой
    * при неверном address выдовать исключение с пояснительной ошибкой
    * следует использовать многопоточность/асинхронность
    * не асинхронные методы не могут блокировать поток
    * данный интерфейс является узким местом и поэтому должн максимально оптимизированн (ИЗБЕГАТЬ создания объектов)
    * при использовании сетевых сокетов и подобных проверять от того ли адреса приходят данные (для функции Accept) - посторонние данные отбрасывать
    * Disconnect по возможности должен собеседнику сообщать об отключении
    * Send игнорирует свойство offset массива и начинает с нуля
    */
    /// <summary>
    /// Делегат для INetworkClient - вызывается когда клиент был отключен
    /// </summary>
    /// <param name="error">код/сообщение/текст ошибки</param>
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
        /// Вызывается при отключении клиента из за внешней ошибки
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
        /// <param name="user_error">ошибка/сообщение отключения</param>
        /// </summary>
        void Disconnect(string user_error);
        /// <summary>
        /// Отправить данные на сервер
        /// </summary>
        /// <param name="array">массив байт для отправки</param>
        /// <param name="guaranteed">гарантированная доставка</param>
        /// <param name="token">токен отмены</param>
        /// <returns></returns>
        Task Send(in INGCArray array, bool guaranteed, CancellationToken token);
        /// <summary>
        /// Ожидает пакет и возвращает размер пакета
        /// </summary>
        /// <param name="token">токен отмены</param>
        /// <returns>размер массива необходимый для считывания</returns>
        Task<int> WaitPacket(CancellationToken token);
        /// <summary>
        /// Считывает пакет (обязательно после WaitPacket)
        /// </summary>
        /// <param name="array">массив куда будет записан пакет</param>
        /// <param name="token">токен отмены операции</param>
        Task AcceptPacket(ref INGCArray array, CancellationToken token);
    }
}
