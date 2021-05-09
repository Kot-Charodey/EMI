using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 21)]
    internal struct BitPacketSegmentedReturned
    {
        [FieldOffset(0)]
        public BitPacketSegmented PacketSegmented;
        [FieldOffset(13)]
        public ulong ReturnID;//айди пакета вызова
    }
}
