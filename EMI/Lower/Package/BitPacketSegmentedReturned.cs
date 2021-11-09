using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 32)] //25
    internal struct BitPacketSegmentedReturned
    {
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ulong ID;//1+8=9
        [FieldOffset(9)]
        public uint Segment;//9+4=13
        [FieldOffset(11)]
        public uint SegmentCount;//13+4=17
        [FieldOffset(13)]
        public ulong ReturnID;//17+8=25    айди пакета вызова
    }
}
