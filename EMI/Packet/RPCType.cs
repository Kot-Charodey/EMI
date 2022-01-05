using System;

namespace EMI.Packet
{
    [Flags]
    internal enum RPCType : byte
    {
        Simple = 32,
        NeedReturn = 64,
        ReturnAnswer = 128,
    }
}
