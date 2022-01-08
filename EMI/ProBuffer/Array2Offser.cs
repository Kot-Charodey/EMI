using System;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <exception cref="ArgumentNullException">массив не указан</exception>
        public Array2Offser(IReleasableArray array, int offset)
        {
            Array = array ?? throw new ArgumentNullException(nameof(array));
            Offset = offset;
        }
    }
}
