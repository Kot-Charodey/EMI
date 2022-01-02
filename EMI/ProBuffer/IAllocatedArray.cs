namespace EMI.ProBuffer
{
    interface IAllocatedArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        int Length { get; }
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        byte[] Array { get; }
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        void Release();
    }
}
