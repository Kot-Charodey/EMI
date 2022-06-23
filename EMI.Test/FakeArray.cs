namespace EMI.Test
{
    /// <summary>
    /// Затычка чтобы не мусорить в буфер <see cref="NGCArray"/>
    /// </summary>
    internal struct FakeArray : INGCArray
    {
        public int Length => Bytes.Length;

        public int Offset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public byte[] Bytes { get; private set; }

        public FakeArray(int size)
        {
            Bytes = new byte[size];
        }

        public void Dispose()
        {
        }
    }
}
