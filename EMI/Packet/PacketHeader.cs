using System;
using System.Runtime.InteropServices;

namespace EMI.Packet
{
    /// <summary>
    /// Заголовок пакета
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
    internal struct PacketHeader
    {
        public const int SizeOf = 2;
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public byte Flags;

        public PacketHeader(PacketType pt)
        {
            PacketType = pt;
            Flags = 0;
        }

        public PacketHeader(RPCType flag)
        {
            PacketType = PacketType.RPC;
            Flags = (byte)flag;
        }

        public PacketHeader(TimeSyncType flag)
        {
            PacketType = PacketType.TimeSync;
            Flags = (byte)flag;
        }

        public PacketHeader(RegisterMethodType flag)
        {
            PacketType = PacketType.RegisterMethod;
            Flags = (byte)flag;
        }
    }
}