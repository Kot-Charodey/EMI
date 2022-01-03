using System.Runtime.InteropServices;

namespace EMI.Packet
{
    /// <summary>
    /// Заголовок пакета
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
    internal struct PacketHeader
    {
        public const int SizeOf = 1;
        public const PacketType NoFlagMask = (PacketType)0b00011111;
        public const PacketType OnlyFlagMask = (PacketType)0b11100000;
        [FieldOffset(0)]
        private PacketType _PacketType;

        public PacketType PacketType
        {
            get => _PacketType & NoFlagMask;
            set => _PacketType = PacketType |= value & NoFlagMask;
        }

        public TimeSyncType TimeSyncType
        {
            get => (TimeSyncType)(_PacketType & OnlyFlagMask);
            set => _PacketType = PacketType | ((PacketType)value & OnlyFlagMask);
        }

        public RegisterMethodType RegisterMethodType
        {
            get => (RegisterMethodType)(_PacketType & OnlyFlagMask);
            set => _PacketType = PacketType | ((PacketType)value & OnlyFlagMask);
        }

        public RPCType RPCType
        {
            get => (RPCType)(_PacketType & OnlyFlagMask);
            set => _PacketType = PacketType | ((PacketType)value & OnlyFlagMask);
        }

        public PacketHeader(PacketType pt, byte flag)
        {
            _PacketType = pt | (PacketType)flag;
        }
    }
}