﻿using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit,Size = 8)] //3
    internal struct BitPacketSimple
    {
        [FieldOffset(0)]
        public PacketType PacketType;//0+1=1
        [FieldOffset(1)]
        public ushort RPCAddres;//1+2=3
    }
}