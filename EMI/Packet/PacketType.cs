namespace EMI.Packet
{
    internal enum PacketType : byte
    {
        Ping05,
        TimeSync,
        RegisterMethod,
        RPC,
    }
}
