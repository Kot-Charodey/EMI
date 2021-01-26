namespace EMI.Lower
{
    internal unsafe struct BitPacketBig
    {
        public PacketType PacketType;
        public ushort ByteDataLength;
        public ushort ID;            
        public int Segment;          
        public int SegmentCount;     
        public fixed byte ByteData[1024];
    }
}
