using System.IO;

namespace EMI
{
    /// <summary>
    /// Дополнительные функции/информация для удалённно вызваного метода
    /// </summary>
    public class MethodHandle
    {
        /// <summary>
        /// Клиент от которого был вызван RPC
        /// </summary>
        public readonly Client Client;
        /// <summary>
        /// Пинг
        /// </summary>
        public float Ping { get; private set; }
        /// <summary>
        /// Связанные сетевые потоки данных
        /// </summary>
        public Stream[] LinkedStreams { get; private set; } = null;
    }
}