using System.Threading;
using System.Threading.Tasks;

namespace EMI.Indicators
{
    using ProBuffer;
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
        protected internal abstract int Size { get; }
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

        internal async Task RCallLow(Client client, RCType type, CancellationToken token)
        {
            bool guarant = type != RCType.Fast && type != RCType.FastForwarding;
            byte packetType;
            if (type == RCType.ReturnWait) {
                packetType = (byte)PacketType.RPC_Return;
            }
            else if (type == RCType.FastForwarding || type == RCType.Forwarding) {
                packetType = (byte)PacketType.RPC_Forwarding;
            }
            else {
                packetType = (byte)PacketType.RPC_Simple;
            }

            IReleasableArray sendArray;
            if (packetType == (byte)PacketType.RPC_Forwarding) //PACK HEADER, init array
            {
                const int bsize = DPack.sizeof_DForwarding + 1;
                sendArray = await client.MyArrayBufferSend.AllocateArrayAsync(bsize + Size, token).ConfigureAwait(false);
                DPack.DForwarding.PackUP(sendArray.Bytes, 1, guarant, ID);
                sendArray.Offset += bsize;
            }
            else
            {
                const int bsize = DPack.sizeof_DRPC + 1;
                sendArray = await client.MyArrayBufferSend.AllocateArrayAsync(bsize + Size, token).ConfigureAwait(false);
                DPack.DRPC.PackUP(sendArray.Bytes, 1, ID);
                sendArray.Offset += bsize;
            }
            sendArray.Bytes[0] = packetType;

            PackUp(sendArray);

            await client.MyNetworkClient.SendAsync(sendArray, guarant, token).ConfigureAwait(false);
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