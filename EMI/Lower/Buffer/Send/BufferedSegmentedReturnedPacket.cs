using System;

namespace EMI.Lower.Buffer.Send
{
    using Package;
    /// <summary>
    /// Буффер для сегментированного пакета
    /// </summary>
    internal class BufferedSegmentedReturnedPacket : IBufferedPackets
    {
        public bool SegmentPacket => false;
        /// <summary>
        /// Заголовок для отправки пакета
        /// </summary>
        private BitPacketSegmentedReturned Header;
        private byte[] Data;

        public BufferedSegmentedReturnedPacket(BitPacketSegmentedReturned packet, byte[] data)
        {
            Header = packet;
            Data = data;
        }

        public unsafe byte[] GetData(uint segment = 0)
        {
            long point = segment * 1024;
            byte[] segmentData = new byte[Math.Min(Data.LongLength - point, 1024)];
            Array.Copy(Data, point, segmentData, 0, segmentData.Length);
            Header.Segment = segment;
            return Packagers.SegmentedReturned.PackUP(Header, segmentData);
        }
    }
}

//