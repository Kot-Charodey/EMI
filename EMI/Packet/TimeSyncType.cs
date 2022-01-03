using System;

namespace EMI.Packet
{
    [Flags]
    internal enum TimeSyncType : byte
    {
        Ticks = 64,
        Integ = 128,
    }
}
