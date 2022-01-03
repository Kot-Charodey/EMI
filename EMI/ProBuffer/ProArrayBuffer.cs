using System.Threading;

namespace EMI.ProBuffer
{
    /// <summary>
    /// Позволяет реиспользовать массивне выделяя новую память
    /// </summary>
    public class ProArrayBuffer
    {
        internal Semaphore Semaphore;
        internal int FreeArrayID;
        internal ReleasableArray [] Arrays;
        private int ArraySize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count">кол-во буферов</param>
        /// <param name="size">размер буфера (если потребуется буфер более большого размера то будет создан новый временный массив)</param>
        public ProArrayBuffer(int count,int size)
        {
            Init(count, size);
        }

        /// <summary>
        /// Пересоздаёт весь буфер заново
        /// </summary>
        public void Reinit()
        {
            Semaphore.Dispose();
            Init(Arrays.Length, ArraySize);
        }

        private void Init(int count, int size)
        {
            Semaphore = new Semaphore(count, count);
            FreeArrayID = count - 1;
            Arrays = new ReleasableArray[count];
            ArraySize = size;

            for (int i = 0; i < count; i++)
            {
                Arrays[i] = new ReleasableArray(this, size);
            }
        }

        /// <summary>
        /// Выделить массивы указанной длинны (поток заблокируется если все массивы заняты)
        /// </summary>
        /// <param name="size">размер массива</param>
        /// <returns></returns>
        public IReleasableArray AllocateArray(int size)
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
