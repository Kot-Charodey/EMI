using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.ProBuffer
{
    /// <summary>
    /// Позволяет реиспользовать массивне выделяя новую память
    /// </summary>
    internal class ProArrayBuffer
    {
        internal readonly Semaphore Semaphore;
        internal int FreeArrayID;
        internal readonly AllocatedArray[] Arrays;
        private readonly int ArraySize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count">кол-во буферов</param>
        /// <param name="size">размер буфера (если потребуется буфер более большого размера то будет создан новый временный массив)</param>
        public ProArrayBuffer(int count,int size)
        {
            Semaphore = new Semaphore(count, count);
            FreeArrayID = count - 1;
            Arrays = new AllocatedArray[count];
            ArraySize = size;

            for(int i = 0; i < count; i++)
            {
                Arrays[i] = new AllocatedArray(this, size);
            }
        }

        public IAllocatedArray AllocateArray(int size)
        {
            if (ArraySize < size)
            {
                return new WrapperArray(size);
            }
            else
            {
                Semaphore.WaitOne();
                lock (Arrays)
                {
                    var array = Arrays[FreeArrayID--];
                    array.Length = size;
                    return array;
                }
            }
        }
    }
}
