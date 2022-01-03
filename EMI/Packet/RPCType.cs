using System;

namespace EMI.Packet
{
    [Flags]
    internal enum RPCType : byte
    {
        Simple = 32, 
        Returnded = 64,
        ReturnAnswer = 128,
    }
}
