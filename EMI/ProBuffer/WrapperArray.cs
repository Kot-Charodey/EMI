namespace EMI.ProBuffer
{
    /// <summary>
    /// Можно использовать в IReleasableArray но выделенный отдельно а не из кучи ProArrayBuffer
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
        /// Смещение - от куда следует считывать
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        public void Release()
        {
            Bytes = null;
        }

        /// <summary>
        /// Инициализировать массив указанного размера
        /// </summary>
        /// <param name="size"></param>
        public WrapperArray(int size)
        {
            Offset = 0;
            Bytes = new byte[size];
        }

        /// <summary>
        /// Инициализировать массив 
        /// </summary>
        /// <param name="array"></param>
        public WrapperArray(byte[] array)
        {
            Offset = 0;
            Bytes = array;
        }
    }
}
