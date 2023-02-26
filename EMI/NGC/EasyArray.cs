namespace EMI.NGC
{
    /// <summary>
    /// Создаёт массив вне буфера (обычный массив), использовать когда массив создаётся на долгий просежуток времени <see cref="NGCArray"/>
    /// </summary>
    public struct EasyArray : INGCArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        public int Length { get; private set; }
        /// <summary>
        /// Смещение - от куда следует считывать
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Инициализировать массив указанной длины
        /// </summary>
        /// <param name="size">размер массива</param>
        public EasyArray(int size)
        {
            Offset = 0;
            if (size != 0)
            {
                Bytes = new byte[size];
                Length = size;
            }
            else
            {
                Bytes = null;
                Length = 0;
            }
        }
        /// <summary>
        /// Обернуть обычный массив
        /// </summary>
        /// <param name="array"></param>
        public EasyArray(byte[] array)
        {
            Offset = 0;
            Length = array.Length;
            Bytes = array;
        }
        /// <summary>
        /// Просто затычка
        /// </summary>
        public void Dispose()
        {
        }
    }
}