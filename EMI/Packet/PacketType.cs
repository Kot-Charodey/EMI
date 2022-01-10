namespace EMI.Packet
{
    internal enum PacketType : byte
    {
        None,
        Ping05,
        TimeSync,
        RegisterMethod,
        RPC,
    }
}
