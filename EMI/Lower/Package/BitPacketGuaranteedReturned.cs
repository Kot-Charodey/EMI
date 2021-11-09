using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 24)] //18
    internal struct BitPacketGuaranteedReturned
    {
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ulong ID;//1+8=9
        [FieldOffset(9)]
        public ulong ReturnID;//9+8=17    айди пакета вызова
        [FieldOffset(17)]
        public bool ReturnNull;//17+1=18
    }
}
