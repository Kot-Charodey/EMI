using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 16)] //9
    internal struct BitPacketReqGetPkgSegmented
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ulong ID;
    }
}
