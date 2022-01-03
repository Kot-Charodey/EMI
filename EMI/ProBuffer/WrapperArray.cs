namespace EMI.ProBuffer
{
    /// <summary>
    /// Массив выделенный ProArrayBuffer (массив не стандартного размера)
    /// </summary>
    public struct WrapperArray : IReleasableArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        public int Length => Bytes.Length;
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        public byte[] Bytes { get; private set; }
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        public void Release(){}
        internal WrapperArray(int size)
        {
            Bytes = new byte[size];
        }
    }
}
