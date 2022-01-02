using System.Threading;

namespace EMI
{
    /// <summary>
    /// Блокирует поток до тех пор пока данные не прийдут (вызов Set) (для последовательных сетевых сообщений)
    /// </summary>
    internal class InputSerialWaiter<T>
    {
        private T Date=default;
        private Semaphore Semaphore = new Semaphore(0, 1);

        public T Get()
        {
            Semaphore.WaitOne();
            return Date;
        }

        public void Set(T data)
        {
            Date = data;
            Semaphore.Release();
        }

        public void Reset()
        {
            Semaphore.Release();
            Semaphore = new Semaphore(0, 1);
            Date = default;
        }
    }
}
