using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = 11)]
    internal struct BitPacketGuaranteed
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ushort RPCAddres;
        [FieldOffset(3)]
        public ulong ID;
    }
}