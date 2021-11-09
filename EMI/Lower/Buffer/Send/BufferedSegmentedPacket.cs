using System;

namespace EMI.Lower.Buffer.Send
{
    using Package;
    /// <summary>
    /// Буффер для сегментированного пакета
    /// </summary>
    internal class BufferedSegmentedPacket : IBufferedPackets
    {
        public bool SegmentPacket => false;
        /// <summary>
        /// Заголовок для отправки пакета
        /// </summary>
        private BitPacketSegmented Header;
        private byte[] Data;

        public BufferedSegmentedPacket(BitPacketSegmented packet,byte[] data)
        {
            Header = packet;
            Data = data;
        }

        public unsafe byte[] GetData(int segment = 0)
        {
            int point = segment * 1024;
            byte[] segmentData = new byte[Math.Min(Data.Length - point, 1024)];
            Array.Copy(Data, point, segmentData, 0, segmentData.Length);
            Header.Segment = segment;
            return Packagers.Segmented.PackUP(Header, segmentData);
        }
    }
}
