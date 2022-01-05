using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace EMI.ProBuffer
{
    /// <summary>
    /// Позволяет реиспользовать массивне выделяя новую память
    /// </summary>
    public class ProArrayBuffer
    {
        private SemaphoreSlim Semaphore;
        private int FreeArrayID;
        private ReleasableArray[] Arrays;
        private int ArraySize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count">кол-во буферов</param>
        /// <param name="size">размер буфера (если потребуется буфер более большого размера то будет создан новый временный массив)</param>
        public ProArrayBuffer(int count, int size)
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
            Semaphore = new SemaphoreSlim(count, count);
            FreeArrayID = count - 1;
            Arrays = new ReleasableArray[count];
            ArraySize = size;

            for (int i = 0; i < count; i++)
            {
                Arrays[i] = new ReleasableArray(this, size);
            }
        }

        /// <summary>
        /// Выделить массивы указанной длинны (поток будет ожидать если все массивы заняты)
        /// </summary>
        /// <param name="size">размер массива</param>
        /// <param name="cancellationToken">токен отмены операции</param>
        /// <returns></returns>
        public async Task<IReleasableArray> AllocateArrayAsync(int size, CancellationToken cancellationToken)
        {
            if (ArraySize < size)
            {
                return new WrapperArray(size);
            }
            else
            {
                //если произойдёт дедлок (долго освобождают массив - мы выделим новый массив)
                var wait = await Semaphore.WaitAsync(10000, cancellationToken).ConfigureAwait(false);
                if (wait)
                {
                    lock (Arrays)
                    {
                        var array = Arrays[FreeArrayID--];
                        array.Length = size;
                        return array;
                    }
                }
                else
                {
#if DEBUG
                    Debug.WriteLine("WARING: произошёл дедлок из за нехватки свободных массивов!");
#endif
                    return new WrapperArray(size);
                }
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
                //если произойдёт дедлок (долго освобождают массив - мы выделим новый массив)
                if (Semaphore.Wait(10000))
                {
                    lock (Arrays)
                    {
                        var array = Arrays[FreeArrayID--];
                        array.Length = size;
                        return array;
                    }
                }
                else
                {
#if DEBUG
                    Debug.WriteLine("WARING: произошёл дедлок из за нехватки свободных массивов!");
#endif
                    return new WrapperArray(size);
                }
            }
        }
    }
}