﻿using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = SizeOf)] //12
    internal struct BitPacketGuaranteed
    {
        public const int SizeOf = 16;
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ushort RPCAddres;//1+2=3
        [FieldOffset(3)]
        public ulong ID;//3+8
    }
}