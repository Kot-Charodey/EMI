using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower
{
    /// <summary>
    /// Безопасный контроллер выдачи ID адресов
    /// </summary>
    internal class SendID_Dispatcher
    {
        private SemaphoreSlim IDReturnLock = new SemaphoreSlim(1, 1);
        private ulong ID;

        /// <summary>
        /// Возвращает новый ID для отправки чего либо а так же блокирует его для остальных потоков (необходимо вызвать UnlockID())
        /// </summary>
        /// <returns></returns>
        public async Task<ulong> GetNewIDAndLock()
        {
            await IDReturnLock.WaitAsync();
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
        public async void SetID(ulong ID)
        {
            await IDReturnLock.WaitAsync();
            this.ID = ID;
            IDReturnLock.Release();
        }

        /// <summary>
        /// Безопасно получить текущий ID
        /// </summary>
        public async Task<ulong> GetID()
        {
            await IDReturnLock.WaitAsync();
            ulong gid = ID;
            IDReturnLock.Release();
            return gid;
        }
    }
}
