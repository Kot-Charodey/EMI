using System;

namespace EMI.Packet
{
    [Flags]
    internal enum RPCType : byte
    {
        Simple,
        NeedReturn,
        ReturnAnswer,
    }
}
