using System.Threading;

namespace EMI.Lower
{
    /// <summary>
    /// Безопасный контроллер выдачи ID адресов
    /// </summary>
    internal class SendID_Dispatcher
    {
        private Semaphore IDReturnLock = new Semaphore(1, 1);
        private ulong ID;

        /// <summary>
        /// Возвращает новый ID для отправки чего либо а так же блокирует его для остальных потоков (необходимо вызвать UnlockID())
        /// </summary>
        /// <returns></returns>
        public ulong GetNewIDAndLock()
        {
            IDReturnLock.WaitOne();
            return ID++;
        }

        /// <summary>
        /// Разблокирует возможность получить ID
        /// </summary>
        public void UnlockID()
        {
            IDReturnLock.Release();
        }

        /// <summary>
        /// Безопасно установить новый ID
        /// </summary>
        /// <param name="ID"></param>
        public void SetID(ulong ID)
        {
            IDReturnLock.WaitOne();
            this.ID = ID;
            IDReturnLock.Release();
        }

        /// <summary>
        /// Безопасно получить текущий ID
        /// </summary>
        public ulong GetID()
        {
            IDReturnLock.WaitOne();
            ulong gid = ID;
            IDReturnLock.Release();
            return gid;
        }
    }
}
