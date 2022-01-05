using System;

namespace EMI.Packet
{
    [Flags]
    internal enum RegisterMethodType : byte
    {
        Add = 32,
        Remove = 64,
        SendList = 128,
    }
}
