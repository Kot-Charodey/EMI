using System.Collections.Generic;
using System.Threading;

namespace EMI
{
    using Lower.Package;

    internal class ReturnWaiter
    {
        private class Waiter
        {
            public bool ClientOut = false;
            public Thread Thread;
            public byte[] Data;
        }

        private readonly Dictionary<ulong, Waiter> list = new Dictionary<ulong, Waiter>();

        /// <summary>
        /// Ожидает ответ и возвращает аргументы
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="ID"></param>
        /// <returns></returns>
        public TOut[] Wait<TOut>(ulong ID)
        {
            Waiter waiter = new Waiter()
            {
                Thread = Thread.CurrentThread
            };
            lock (list)
            {
                list.Add(ID, waiter);
            }
            try
            {
                Thread.Sleep(Timeout.Infinite);
            }
            catch { }

            if (waiter.ClientOut)
            {
                throw new ClientOutException();
            }

            TOut[] datas = null;
            fixed (byte* bytes = &waiter.Data[0])
            {
                PackageReturned* package = (PackageReturned*)bytes;
                int sm = sizeof(PackageReturned);
                int sizeArg = sizeof(BitArgument);
                BitArgument* argument = (BitArgument*)(bytes + sm);

                datas = new TOut[package->ArgumentCount];
                for (int i = 0; i < datas.Length; i++)
                {
                    datas[i] = *((TOut*)(bytes + sm + sizeArg));
                    sm += argument->Size;
                    argument = (BitArgument*)(bytes + sm);
                }
            }
            return datas;
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
                wait.Data = data;
                wait.Thread.Interrupt();
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
                    waiter.Thread.Interrupt();
                }
            }
        }
    }
}