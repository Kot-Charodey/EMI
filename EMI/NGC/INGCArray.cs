using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.NGC
{
    /// <summary>
    /// Массив с вынесенной переменной длинны и смещения
    /// </summary>
    public interface INGCArray : IDisposable
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        int Length { get; }
        /// <summary>
        /// Смещение - от куда следует считывать
        /// </summary>
        int Offset { get; set; }
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        byte[] Bytes { get; }
    }
}