using System;

namespace EMI.Network.NetTCPV3
{
    /// <summary>
    /// Флаги сообщения в пакете
    /// </summary>
    [Flags]
    internal enum PacketHeader : byte
    {
        None = 0,
        Error = 16,
        PacketDone = 32,
        SegmentPacketHeader = 64,
        SegmentPacketElement = 128,
    }
}