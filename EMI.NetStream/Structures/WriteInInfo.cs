namespace EMI.NetStream.Structures
{
    internal struct WriteInInfo
    {
        public int Offset;
        public int Count;
        public byte[] Buffer;

        public WriteInInfo(int offset, int count, byte[] buffer)
        {
            Offset = offset;
            Count = count;
            Buffer = buffer;
        }
    }
}
