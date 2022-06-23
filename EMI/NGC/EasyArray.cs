namespace EMI.NGC
{
    /// <summary>
    /// Создаёт массив вне буфера (обычный массив), использовать когда массив создаётся на долгий просежуток времени <see cref="NGCArray"/>
    /// </summary>
    internal struct EasyArray : INGCArray
    {
        public int Length => Bytes.Length;

        public int Offset { get; set; }

        public byte[] Bytes { get; private set; }

        public EasyArray(int size)
        {
            Offset = 0;
            Bytes = new byte[size];
        }

        public void Dispose()
        {
            Bytes = null;
        }
    }
}