namespace EMI.Lower
{
    using Package;

    internal class AcceptData
    {
        public PacketType PacketType;
        public int Size;
        public byte[] Buffer;

        public AcceptData(int size, byte[] buffer)
        {
            PacketType = buffer.GetPacketType();
            Size = size;
            Buffer = buffer;
        }
    }
}