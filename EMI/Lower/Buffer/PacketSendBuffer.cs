using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace EMI.Lower.Buffer
{
    using Package;
    using Send;

    /// <summary>
    /// Хранит пакеты до окончательной доставки пользователю
    /// </summary>
    internal class PacketSendBuffer
    {
        internal struct Delivery
        {
            public ulong ID;
            public byte[] BytePacket;
        }

        /// <summary>
        /// Если пакетов больше данного числа то поток Storing заблокируется в ожидание свободного
        /// </summary>
        public const int Capacity = 128;
        /// <summary>
        /// Буффер отпраленых пакетов
        /// </summary>
        private Dictionary<ulong, IBufferedPackets> Buffer = new Dictionary<ulong, IBufferedPackets>();
        /// <summary>
        /// Буффер для сообщений о подтверждении доставленных пакетов
        /// должен очищаться когда ID пакета старее чем самый старый пакет который не пришёл 
        /// в отличии от основного буфера не блокирует поток
        /// </summary>
        private List<Delivery> BufferDelivery = new List<Delivery>();
        /// <summary>
        /// Используется как счётсчик пакетов (если в буфере их больше положеного, блокирует поток)
        /// </summary>
        private SemaphoreSlim BufferPlace = new SemaphoreSlim(Capacity, Capacity);
        private AcceptID_Dispatcher AcceptID;

        public PacketSendBuffer(AcceptID_Dispatcher acceptID_Dispatcher)
        {
            AcceptID = acceptID_Dispatcher;
        }

        /// <summary>
        /// Убирает из буфера пакеты которые точно пришли
        /// </summary>
        private void ClearBufferDelivery()
        {
            const int CapacityOLD = Capacity + 5;
            ulong oldID = AcceptID.GetDataLastID() - CapacityOLD;

            lock (BufferDelivery)
            {
                for (int i = 0; i < BufferDelivery.Count; i++)
                {
                    if (BufferDelivery[i].ID < oldID)
                        BufferDelivery.RemoveAt(i--);
                }
            }
        }

        public async Task Storing(ulong ID, byte[] data)
        {
            ClearBufferDelivery();

            await BufferPlace.WaitAsync();
            if (data.GetPacketType().IsSegmentPacket())
                throw new System.Exception("EMI -> PacketSendBuffer -> Storing -> if GetPacketType().IsSegmentPacket()");

            lock (Buffer)
            {
                if (data.GetPacketType().IsDeliveryСompletedPackage())
                {
                    lock (BufferDelivery)
                        BufferDelivery.Add(new Delivery() { ID = ID, BytePacket = data });
                    BufferPlace.Release();
                }
                else
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
                    lock (BufferDelivery)
                    {
                        for(int i = 0; i < BufferDelivery.Count; i++)
                        {
                            if (BufferDelivery[i].ID == ID)
                            {
                                return BufferDelivery[i].BytePacket;
                            }
                        }
                    }
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