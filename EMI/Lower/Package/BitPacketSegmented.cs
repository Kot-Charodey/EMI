using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 15)]
    internal struct BitPacketSegmented
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ulong ID;
        [FieldOffset(9)]
        public ushort Segment;
        [FieldOffset(11)]
        public ushort SegmentCount;
        [FieldOffset(13)]
        public ushort RPCAddres;
    }
}
