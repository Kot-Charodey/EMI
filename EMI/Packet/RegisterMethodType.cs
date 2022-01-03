using System;

namespace EMI.Packet
{
    [Flags]
    internal enum RegisterMethodType : byte
    {
        Request = 64,
        Answer = 128,
    }
}
