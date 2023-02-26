namespace EMI.NGC
{
    /// <summary>
    /// Расширения для <see cref="INGCArray"/>
    /// </summary>
    public static class INGCArrayUtils
    {
        /// <summary>
        /// Пустой массив
        /// </summary>
        public static readonly INGCArray EmptyArray = new EasyArray(0);
        /// <summary>
        /// Пустой ли массив
        /// </summary>
        /// <param name="array">массив</param>
        /// <returns><see cref="INGCArray.Bytes"/> == null</returns>
        public static bool IsEmpty(this INGCArray array)
        {
            return array.Bytes == null;
        }
    }
}