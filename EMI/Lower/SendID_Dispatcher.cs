using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower
{
    /// <summary>
    /// Безопасный контроллер выдачи ID адресов
    /// </summary>
    internal class SendID_Dispatcher
    {
        private ulong ID;

        /// <summary>
        /// Возвращает новый ID для отправки чего либо
        /// </summary>
        /// <returns></returns>
        public ulong GetNewID()
        {
            lock (this)
            {
                return ID++;
            }
        }

        /// <summary>
        /// Безопасно получить текущий ID (по данному ID будет отправленно след сообщение)
        /// </summary>
        /// <returns></returns>
        public ulong GetID()
        {
            lock (this)
            {
                return ID;
            }
        }
    }
}
