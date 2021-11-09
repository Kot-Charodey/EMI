using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace EMI.Lower.Buffer
{
    using Package;
    using Send;

    /// <summary>
    /// Хранит пакеты до окончательной доставки пользователю
    /// </summary>
    internal class PacketSendBuffer
    {
        /// <summary>
        /// Если пакетов больше данного числа то поток Storing заблокируется в ожидание свободного
        /// </summary>
        public const int Capacity = 128;
        private Dictionary<ulong, IBufferedPackets> Buffer = new Dictionary<ulong, IBufferedPackets>();
        /// <summary>
        /// Используется как счётсчик пакетов (если в буфере их больше положеного, блокирует поток)
        /// </summary>
        private SemaphoreSlim BufferPlace = new SemaphoreSlim(Capacity, Capacity);

        public async Task Storing(ulong ID, byte[] data)
        {
            await BufferPlace.WaitAsync();
            if (data.GetPacketType().IsSegmentPacket())
                throw new System.Exception("EMI -> PacketSendBuffer -> Storing -> if GetPacketType().IsSegmentPacket()");

            lock (Buffer)
            {
                Buffer.Add(ID, new BufferedShortPacket(data));
            }
        }

        public async Task Storing(ulong ID, BitPacketSegmented bitPacket, byte[] data)
        {
            await BufferPlace.WaitAsync();

            lock (Buffer)
            {
                Buffer.Add(ID, new BufferedSegmentedPacket(bitPacket, data));
            }
        }

        public async Task Storing(ulong ID, BitPacketSegmentedReturned bitPacket, byte[] data)
        {
            await BufferPlace.WaitAsync();

            lock (Buffer)
            {
                Buffer.Add(ID, new BufferedSegmentedReturnedPacket(bitPacket, data));
            }
        }

        public byte[] GetPacket(ulong ID, int segment = 0)
        {
            lock (Buffer)
            {
                if (Buffer.TryGetValue(ID, out IBufferedPackets packet))
                {
                    return packet.GetData(segment);
                }
                else
                {
                    return null;
                }
            }
        }

        public void RemovePacket(ulong ID)
        {
            lock (Buffer)
            {
                if (Buffer.ContainsKey(ID))
                {
                    Buffer.Remove(ID);
                    BufferPlace.Release();
                }
            }
        }
    }
}