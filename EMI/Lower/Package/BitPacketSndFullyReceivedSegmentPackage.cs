using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = SizeOf)] // 17
    internal struct BitPacketSndFullyReceivedSegmentPackage
    {
        public const int SizeOf = 24;
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ulong ID;//1+8=9
        [FieldOffset(9)]
        public ulong FullID;//9+8=17 айди доставленного пакета
    }
}