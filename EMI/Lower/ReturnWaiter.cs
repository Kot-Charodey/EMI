using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower
{
    using SmartPackager;
    using System;

    internal class ReturnWaiter
    {
        private class Waiter
        {
            public bool ClientOut = false;
            public SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(0, 1);
            public byte[] Data;
        }

        private readonly Dictionary<ulong, Waiter> list = new Dictionary<ulong, Waiter>();

        private class Handle<TOut> {
            
            public Waiter Waiter;

            public async Task<TOut> Wait()
            {
                var pack = Packager.Create<TOut>();

                //когда прийдёт наш пакет то поток разбудят и мы его отдадим
                await Waiter.SemaphoreSlim.WaitAsync().ConfigureAwait(false);

                if (Waiter.ClientOut)
                {
                    throw new ClientOutException();
                }

                pack.UnPack(Waiter.Data, 0, out TOut ret);
                return ret;
            }
        }

        
        /// <summary>
        /// Генерирует функцию ожидания - (её можно запустить позже)
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="ID"></param>
        /// <returns></returns>
        public RPCfunctOut<Task<TOut>> SetupWaiting<TOut>(ulong ID)
        {
            Handle<TOut> handle = new Handle<TOut>();
            handle.Waiter = new Waiter();
            lock (list)
            {
                list.Add(ID, handle.Waiter);
            }

            return handle.Wait;
        }

        /// <summary>
        /// Разблокирует поток и передаёт аргументы
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="data"></param>
        public void AddData(ulong ID, byte[] data)
        {

            lock (list)
            {
                var wait = list[ID];
                list.Remove(ID);
                wait.Data = data ?? throw new ArgumentNullException(nameof(data));
                wait.SemaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Будит все потоки и отсылает туда ошибку ClientOutException
        /// </summary>
        public void ErrorStop()
        {
            lock (list)
            {
                foreach (Waiter waiter in list.Values)
                {
                    waiter.ClientOut = true;
                    waiter.SemaphoreSlim.Release();
                }
            }
        }
    }
}