using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 9)]
    internal struct BitPacketGetPkg
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ulong ID;
    }
}