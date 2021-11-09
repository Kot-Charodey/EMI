using System.Collections.Generic;

namespace EMI.Lower.Buffer.Accept
{
    using Package;

    internal abstract class ABufferedPackets
    {
        private PacketInfo PacketInfo;

        protected uint SegmentCount;
        private HashSet<uint> Delivered = new HashSet<uint>();

        public ABufferedPackets(BitPacketSegmented packet)
        {
            PacketInfo.PacketType = packet.PacketType;
            PacketInfo.ID = packet.ID;
            PacketInfo.RPCAddres = packet.RPCAddres;
            SegmentCount = packet.SegmentCount;

            InitDataBuffer(SegmentCount);
        }

        public ABufferedPackets(BitPacketSegmentedReturned packet)
        {
            PacketInfo.PacketType = packet.PacketType;
            PacketInfo.ID = packet.ID;
            PacketInfo.ReturnID = packet.ReturnID;
            SegmentCount = packet.SegmentCount;

            InitDataBuffer(SegmentCount);
        }

        public bool IsReady()
        {
            return SegmentCount == Delivered.Count;
        }

        public void WriteSegment(uint segment, byte[] data)
        {
            if (!Delivered.Contains(segment))
            {
                WriteSegmentF(segment, data);
                Delivered.Add(segment);
            }
        }

        public PacketInfo GetPacketInfo()
        {
            PacketInfo info = PacketInfo;
            info.Data = GetDataF(out info.DataLength);
            return PacketInfo;
        }

        protected abstract void InitDataBuffer(uint SegmentCount);
        protected abstract void WriteSegmentF(uint segment, byte[] data);
        protected abstract byte[] GetDataF(out uint DataLength);
    }
}
