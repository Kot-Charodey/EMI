namespace EMI.Network
{
    /* Рекомендации по реализации интерфейса
     * Интерфейс должен реализовать фабрику по производству объектов INetworkClient и INetworkServer
     * реализовать статическое поле Service выдающие копию интерфейса
     */

    /// <summary>
    /// Сервис предоставляет доступ к новым объектам INetworkClient и INetworkServer
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// Создать новый клиент
        /// </summary>
        INetworkClient GetNewClient();
        /// <summary>
        /// Создать новый сервер
        /// </summary>
        INetworkServer GetNewServer();
    }
}
