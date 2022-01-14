namespace EMI.Lower
{
    using Package;

    internal struct AcceptData
    {
        public PacketType PacketType;
        public int Size;
        public byte[] Buffer;

        public AcceptData(int size, byte[] buffer)
        {
            Size = size;
            Buffer = buffer;
            PacketType = Buffer.GetPacketType();
        }
    }
}