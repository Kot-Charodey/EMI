using System.IO;

namespace EMI
{
    /// <summary>
    /// Дополнительные функции/информация для удалённно вызваного метода
    /// </summary>
    public struct MethodHandle
    {
        /// <summary>
        /// Клиент от которого был вызван RPC
        /// </summary>
        public Client Client;
        /// <summary>
        /// Пинг
        /// </summary>
        public float Ping { get; private set; }
        /// <summary>
        /// Связанные сетевые потоки данных
        /// </summary>
        public Stream[] LinkedStreams { get; private set; }
    }
}