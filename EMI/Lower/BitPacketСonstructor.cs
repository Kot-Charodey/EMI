using System;
using System.Runtime.InteropServices;

namespace EMI.Lower
{
    internal static class BitPacketСonstructor
    {
        public PacketType PacketType;
        public ushort ByteDataLength;
        public ulong ID;
        public ulong MainID;
        public ushort Segment;
        public ushort SegmentCount;
        public byte ByteData[1024];
    }
}
