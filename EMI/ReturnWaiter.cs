using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    using Lower.Package;
    using SmartPackager;

    internal class ReturnWaiter
    {
        private class Waiter
        {
            public bool ClientOut = false;
            public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
            public byte[] Data;
        }

        private readonly Dictionary<ulong, Waiter> list = new Dictionary<ulong, Waiter>();

        /// <summary>
        /// Ожидает ответ и возвращает аргументы
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="ID"></param>
        /// <param name="send"></param>
        /// <returns></returns>
        public async Task<TOut> Wait<TOut>(ulong ID, RPCfunct send)
        {
            var pack = Packager.Create<TOut>();

            //оставим запрос и идём спать - когда прийдёт наш пакет то поток разбудят
            Waiter waiter = new Waiter();
            lock (list)
            {
                list.Add(ID, waiter);
            }
            send.Invoke();
            try
            {
                //ждём пока прийдёт ответ....
                await Task.Delay(-1, waiter.CancellationTokenSource.Token);
            }
            catch { }

            if (waiter.ClientOut)
            {
                throw new ClientOutException();
            }

            pack.UnPack(waiter.Data, 0, out TOut ret);
            return ret;
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
                wait.CancellationTokenSource.Cancel();
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
                    waiter.CancellationTokenSource.Cancel(true);
                }
            }
        }
    }
}