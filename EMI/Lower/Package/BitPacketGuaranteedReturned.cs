using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 24)] //18
    internal struct BitPacketGuaranteedReturned
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ulong ID;
        [FieldOffset(9)]
        public ulong ReturnID;//айди пакета вызова
        [FieldOffset(17)]
        public bool ReturnNull;
    }
}
