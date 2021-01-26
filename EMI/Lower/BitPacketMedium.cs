namespace EMI.Lower
{
    internal unsafe struct BitPacketMedium
    {
        public PacketType PacketType;
        public ushort ByteDataLength;
        public ushort ID;
        public fixed byte ByteData[1024];
    }
}
