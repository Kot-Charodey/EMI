namespace EMI.ProBuffer
{
    /// <summary>
    /// Массив выделенный ProArrayBuffer
    /// </summary>
    public struct ReleasableArray:IReleasableArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        public int Length { get; internal set; }
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        public byte[] Bytes { get; private set; }
        /// <summary>
        /// Смещение - от куда следует считывать
        /// </summary>
        public int Offset { get; set; }

        private readonly ProArrayBuffer MyBuffer;
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        public void Release()
        {
            lock (MyBuffer.Arrays)
            {
                MyBuffer.FreeArrayID++;
            }

            Offset = 0;
            MyBuffer.Semaphore.Release();
        }
        internal ReleasableArray (ProArrayBuffer myBuffer, int size)
        {
            Bytes = new byte[size];
            MyBuffer = myBuffer;
            Offset = 0;
            Length = 0;
        }
    }
}
