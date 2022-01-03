namespace EMI.ProBuffer
{
    /// <summary>
    /// Массив и смещение от куда чтоит считывать данные
    /// </summary>
    public struct Array2Offser
    {
        /// <summary>
        /// Массив
        /// </summary>
        public IReleasableArray Array;
        /// <summary>
        /// Смещение
        /// </summary>
        public int Offset;
    }
}
