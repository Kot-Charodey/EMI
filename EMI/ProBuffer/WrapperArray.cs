namespace EMI.ProBuffer
{
    /// <summary>
    /// Массив выделенный ProArrayBuffer (массив не стандартного размера)
    /// </summary>
    public struct WrapperArray : IAllocatedArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        public int Length => Array.Length;
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        public byte[] Array { get; private set; }
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        public void Release(){}
        internal WrapperArray(int size)
        {
            Array = new byte[size];
        }
    }
}
