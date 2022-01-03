using System;

namespace EMI.Packet
{
    [Flags]
    internal enum RegisterMethodType : byte
    {
        Request = 32,
        Answer = 64,
        BadAnswer = 128,
    }
}
