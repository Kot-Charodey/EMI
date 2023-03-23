namespace EMI.NetStream.Structures
{
    internal struct ReadInInfo
    {
        public int BufferSize;
        public int Offset;
        public int Count;

        public ReadInInfo(int bufferSize, int offset, int count)
        {
            BufferSize = bufferSize;
            Offset = offset;
            Count = count;
        }
    }
}
