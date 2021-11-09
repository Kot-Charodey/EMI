using System.Collections.Generic;

namespace EMI.Lower.Buffer.Accept
{
    using Package;

    internal abstract class ABufferedPackets
    {
        private PacketInfo PacketInfo;

        protected int SegmentCount;
        private HashSet<int> Delivered = new HashSet<int>();

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

        public void WriteSegment(int segment, byte[] data)
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
            return info;
        }

        public int[] GetDownloadList(int count)
        {
            int minStart = 0;
            List<int> list = new List<int>(count + 1);
            for (int i = 0; i < count && minStart < SegmentCount; i++)
            {
                do
                {
                    if (Delivered.Contains(minStart))
                    {
                        minStart++;
                    }
                    else
                    {
                        list.Add(minStart++);
                        goto @continue;
                    }
                } while (minStart < SegmentCount);
                break;
            @continue:;
            }
            return list.ToArray();
        }

        protected abstract void InitDataBuffer(int SegmentCount);
        protected abstract void WriteSegmentF(int segment, byte[] data);
        protected abstract byte[] GetDataF(out int DataLength);
    }
}
