namespace EMI.Lower
{
    internal unsafe struct BitPacketSimple
    {
        public PacketType PacketType;
        public ushort ByteDataLength;
        public fixed byte ByteData[1024];
    }
}
