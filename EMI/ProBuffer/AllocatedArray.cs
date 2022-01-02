namespace EMI.ProBuffer
{
    /// <summary>
    /// Массив выделенный ProArrayBuffer
    /// </summary>
    public struct AllocatedArray:IAllocatedArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        public int Length { get; internal set; }
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        public byte[] Array { get; private set; }
        private ProArrayBuffer MyBuffer;
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        public void Release()
        {
            lock (MyBuffer.Arrays)
            {
                MyBuffer.FreeArrayID++;
            }
            MyBuffer.Semaphore.Release();
        }
        internal AllocatedArray(ProArrayBuffer myBuffer, int size) : this()
        {
            Array = new byte[size];
            MyBuffer = myBuffer;
        }
    }
}
