namespace EMI
{
    internal enum PacketType : byte
    {
        None,

        Ping_Send,
        Ping_Receive,

        RPC_Simple,
        RPC_Return,
        RPC_Returned,
        RPC_Forwarding,
    }
}
