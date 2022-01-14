using System;

namespace EMI.Lower.Buffer.Accept
{
    using Package;

    internal class BufferedByteArrayPacket : ABufferedPackets
    {
        private int SizeData;
        private byte[] Data;

        public BufferedByteArrayPacket(BitPacketSegmented packet) : base(packet)
        {
        }

        public BufferedByteArrayPacket(BitPacketSegmentedReturned packet) : base(packet)
        {
        }

        protected override void InitDataBuffer(int SegmentCount)
        {
            Data = new byte[SegmentCount * 1024];
        }

        protected override void WriteSegmentF(int segment, byte[] data)
        {
            SizeData += data.Length;
            long point = segment * 1024;
            Array.Copy(data, 0, Data, point, data.Length);
        }

        protected override byte[] GetDataF(out int DataLength)
        {
            DataLength = SizeData;
            return Data;
        }
    }
}
