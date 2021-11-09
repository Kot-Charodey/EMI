using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = 24)] // 17
    internal struct BitPacketSndFullyReceivedSegmentPackage
    {
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ulong ID;//1+8=9
        [FieldOffset(9)]
        public ulong FullID;//9+8=17 айди доставленного пакета
    }
}