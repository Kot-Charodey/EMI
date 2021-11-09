using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = SizeOf)] //19
    internal struct BitPacketSegmented
    {
        public const int SizeOf = 24;
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ulong ID;//1+8=9
        [FieldOffset(9)]
        public int Segment;//9+4=13
        [FieldOffset(13)]
        public int SegmentCount;//13+4=17
        [FieldOffset(17)]
        public ushort RPCAddres;//17+2=19
    }
}
