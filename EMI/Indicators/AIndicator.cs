using System;
using System.Threading;
using System.Threading.Tasks;

using SmartPackager;

namespace EMI.Indicators
{
    using ProBuffer;
    using MyException;
    /// <summary>
    /// Ссылка на удалённый метод
    /// </summary>
    public abstract class AIndicator
    {
        /// <summary>
        /// айди вызываймой функции
        /// </summary>
        protected internal int ID;
        /// <summary>
        /// Размер необходимый для упаковки параметров
        /// </summary>
        protected internal abstract int Size { get;}
        /// <summary>
        /// Упаковщик
        /// </summary>
        /// <param name="array"></param>
        protected internal abstract void PackUp(IReleasableArray array);
        /// <summary>
        /// Распаковщик
        /// </summary>
        /// <param name="array"></param>
        protected internal abstract void UnPack(IReleasableArray array);

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="methodName">имя метода на который ссылается ссылка (функция не будет вызвана если имя указано не полностью)(namespace.class.method)</param>
        internal AIndicator(string methodName)
        {
            ID = methodName.DeterministicGetHashCode();
        }

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="method"></param>
        internal AIndicator(Delegate method)
        {
            ID = RPC.GetDelegateName(method).DeterministicGetHashCode();
        }

        internal async Task RCallLow(Client client, RCType type,CancellationToken token)
        {
            const int bsize = DPack.sizeof_DRPC + 1;
            int size = bsize + Size;
            var sendArray = await client.MyArrayBufferSend.AllocateArrayAsync(size, token).ConfigureAwait(false);
            sendArray.Bytes[0] = type == RCType.ReturnWait ? (byte)PacketType.RPC_Return : (byte)PacketType.RPC_Simple;
            DPack.DRPC.PackUP(sendArray.Bytes, 1, ID);
            sendArray.Offset += bsize;
            PackUp(sendArray);

            await client.MyNetworkClient.SendAsync(sendArray, type != RCType.Fast, token).ConfigureAwait(false);
            if (type == RCType.ReturnWait)
            {
                RCWaitHandle handle = new RCWaitHandle();
                var tokenPro = CancellationTokenSource.CreateLinkedTokenSource(token, client.CancellationRun.Token).Token;

                lock (client.RPCReturn)
                {
                    client.RPCReturn.Add(ID, handle);
                }
                await handle.Semaphore.WaitAsync(tokenPro).ConfigureAwait(false);
                UnPack(handle.Array);
                handle.Array.Release();
            }
            sendArray.Release();
        }
    }
}