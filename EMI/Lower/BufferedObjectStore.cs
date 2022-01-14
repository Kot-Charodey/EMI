using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower
{
    /// <summary>
    /// Позволяет использховать несколько копий объекта для многопоточности не алоцируя память каждый раз под создание объекта
    /// </summary>
    /// <typeparam name="T">тип данных</typeparam>
    internal class BufferedObjectStore<T> where T: class
    {
        public struct Handle
        {
            public readonly T Object;
            private BufferedObjectStore<T> Store;
            private int ID;

            internal Handle(T @object, BufferedObjectStore<T> store, int id)
            {
                Object = @object;
                Store = store;
                ID = id;
            }

            public void Release()
            {
                lock (Store.InUseT)
                {
                    Store.SemaphoreWaitUse.Release();
                    Store.InUseT[ID] = false;
                    Store.ReleaseIndex = ID;
                }
            }
        }

        private Handle[] StoreT;
        /// <summary>
        /// если true то данный StoreT[i] занят
        /// </summary>
        private bool[] InUseT;
        private SemaphoreSlim SemaphoreWaitUse;
        /// <summary>
        /// Если не -1 то хранит индес точно свободного объекта в StoreT
        /// </summary>
        private int ReleaseIndex=-1;

        /// <summary>
        /// Создаёт новую копию буфера с объектами
        /// </summary>
        /// <param name="create">Инициализатор объекта</param>
        /// <param name="maxParallelUse">Колличество объектов доступных паралельно</param>
        public BufferedObjectStore(int maxParallelUse, Func<T> create)
        {
            StoreT = new Handle[maxParallelUse];
            InUseT = new bool[maxParallelUse];
            SemaphoreWaitUse = new SemaphoreSlim(maxParallelUse, maxParallelUse);
            for (int i = 0; i < StoreT.Length; i++)
            {
                StoreT[i] = new Handle(create.Invoke(), this, i);
            }
        }

        /// <summary>
        /// Пытается получить свободный объект (блокируя его), если такого нет поток будет асинхронно ждать освобождение какого либо
        /// </summary>
        /// <returns>объект</returns>
        public Handle GetObject()
        {
            SemaphoreWaitUse.Wait();
            int id = -1;
            lock (InUseT)
            {
                if (ReleaseIndex != -1)//если мы знаем что есть свободный ID
                {
                    InUseT[ReleaseIndex] = true;
                    id = ReleaseIndex;
                    ReleaseIndex = -1;
                }
                else//ищим свободный ID
                {
                    for (int i = 0; i < InUseT.Length; i++)
                    {
                        if (!InUseT[i])
                        {
                            id = i;
                            InUseT[i] = true;
                            break;
                        }
                    }
                }
            }

            return StoreT[id];
        }

        /// <summary>
        /// Пытается получить свободный объект (блокируя его), если такого нет поток будет асинхронно ждать освобождение какого либо
        /// </summary>
        /// <returns>объект</returns>
        public async Task<Handle> GetObjectAsync()
        {
            await SemaphoreWaitUse.WaitAsync();
            int id = -1;
            lock (InUseT)
            {
                if (ReleaseIndex != -1)//если мы знаем что есть свободный ID
                {
                    InUseT[ReleaseIndex] = true;
                    id = ReleaseIndex;
                    ReleaseIndex = -1;
                }
                else//ищим свободный ID
                {
                    for (int i = 0; i < InUseT.Length; i++)
                    {
                        if (!InUseT[i])
                        {
                            id = i;
                            InUseT[i] = true;
                            break;
                        }
                    }
                }
            }

            return StoreT[id];
        }
    }
}
