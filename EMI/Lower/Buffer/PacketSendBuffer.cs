using System.Collections.Generic;
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
        private const int Capacity = 128;
        private Dictionary<ulong, IBufferedPackets> Buffer=new Dictionary<ulong, IBufferedPackets>();
        /// <summary>
        /// Используется как счётсчик пакетов (если в буфере их больше положеного, блокирует поток)
        /// </summary>
        private SemaphoreSlim BufferPlace = new SemaphoreSlim(Capacity, Capacity);

        public async void Storing(ulong ID,byte[] data)
        {
            await BufferPlace.WaitAsync();

            lock (Buffer)
            {
                Buffer.Add(ID, new BufferedShortPacket(data));
            }
        }

        public async void Storing(ulong ID, BitPacketSegmented bitPacket, byte[] data)
        {
            await BufferPlace.WaitAsync();

            lock (Buffer)
            {
                Buffer.Add(ID, new BufferedSegmentedPacket(bitPacket, data));
            }
        }

        public async void Storing(ulong ID, BitPacketSegmentedReturned bitPacket, byte[] data)
        {
            await BufferPlace.WaitAsync();

            lock (Buffer)
            {
                Buffer.Add(ID, new BufferedSegmentedReturnedPacket(bitPacket, data));
            }
        }

        public byte[] GetPacket(ulong ID,uint segment = 0)
        {
            lock (Buffer)
            {
                return Buffer[ID].GetData(segment);
            }
        }

        public void RemovePacket(ulong ID)
        {
            BufferPlace.Release();
        }
    }
}
