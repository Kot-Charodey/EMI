using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = 24)] // 17
    internal struct BitPacketSndFullyReceivedSegmentPackage
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ulong ID;
        [FieldOffset(9)]
        public ulong FullID; //айди доставленного пакета
    }
}