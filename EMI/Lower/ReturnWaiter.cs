using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower
{
    using SmartPackager;
    using Buffer;

    internal class ReturnWaiter
    {
        private class Waiter
        {
            private SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(0, 1);
            private byte[] Data;

            public void ReleaseError()
            {
                Data = null;
                SemaphoreSlim.Release();
            }

            public void Release(byte[] data)
            {
                Data = data;
                SemaphoreSlim.Release();
            }

            public async Task<byte[]> WaitAndGet()
            {
                await SemaphoreSlim.WaitAsync().ConfigureAwait(false);
                if(Data==null)
                    throw new ClientOutException();
                return Data;
            }
        }

        private readonly Dictionary<ulong, Waiter> list = new Dictionary<ulong, Waiter>();
        private readonly BufferedObjectStore<Waiter> Waiters = new BufferedObjectStore<Waiter>(PacketSendBuffer.Capacity, () => new Waiter());

        private interface IHandle
        {
            
        }

        private struct Handle<TOut>:IHandle {
            
            public BufferedObjectStore<Waiter>.Handle Waiter;

            public async Task<TOut> Wait()
            {
                var pack = Packager.Create<TOut>();
                TOut ret;

                //когда прийдёт наш пакет то поток разбудят и мы его отдадим
                try
                {
                    pack.UnPack(await Waiter.Object.WaitAndGet().ConfigureAwait(false), 0, out ret);
                }
                finally
                {
                    Waiter.Release();
                }
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
            handle.Waiter = Waiters.GetObject();
            lock (list)
            {
                list.Add(ID, handle.Waiter.Object);
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
                wait.Release(data);
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
                    waiter.ReleaseError();
                }
            }
        }
    }
}