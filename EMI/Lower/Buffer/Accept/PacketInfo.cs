namespace EMI.Lower.Buffer.Accept
{
    internal struct PacketInfo
    {
        public PacketType PacketType;
        public ulong ID;
        public ulong ReturnID;
        public int DataLength;
        public ushort RPCAddres;
        public byte[] Data;
    }
}
