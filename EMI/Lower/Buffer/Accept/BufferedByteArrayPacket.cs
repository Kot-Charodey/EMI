using System;

namespace EMI.Lower.Buffer.Accept
{
    using Package;

    internal class BufferedByteArrayPacket : ABufferedPackets
    {
        private uint SizeData;
        private byte[] Data;

        public BufferedByteArrayPacket(BitPacketSegmented packet) : base(packet)
        {
        }

        public BufferedByteArrayPacket(BitPacketSegmentedReturned packet) : base(packet)
        {
        }

        protected override void InitDataBuffer(uint SegmentCount)
        {
            Data = new byte[SegmentCount * 1024];
        }

        protected override void WriteSegmentF(uint segment, byte[] data)
        {
            SizeData += (uint)data.Length;
            long point = segment * 1024;
            Array.Copy(data, 0, Data, point, data.Length);
        }

        protected override byte[] GetDataF(out uint DataLength)
        {
            DataLength = SizeData;
            return Data;
        }
    }
}
