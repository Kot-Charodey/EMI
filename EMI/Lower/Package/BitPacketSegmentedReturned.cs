using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 23)]
    internal struct BitPacketSegmentedReturned
    {
        [FieldOffset(0)]
        public BitPacketSegmented PacketSegmented;
        [FieldOffset(15)]
        public ulong ReturnID;//айди пакета вызова
    }
}
