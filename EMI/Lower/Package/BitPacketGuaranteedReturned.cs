using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 17)]
    internal struct BitPacketGuaranteedReturned
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ulong ID;
        [FieldOffset(9)]
        public ulong ReturnID;//айди пакета вызова
    }
}
