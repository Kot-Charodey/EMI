using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EMI.ProBuffer
{
    /// <summary>
    /// Позволяет реиспользовать массивне выделяя новую память
    /// </summary>
    public class ProArrayBuffer
    {
        internal static TimeSpan ReleasableArrayLifetime = new TimeSpan(0, 1, 0);
        internal static TimeSpan AllocadedArrayMaxLifetime = new TimeSpan(0, 5, 0);

        internal List<ReleasableArray> ReleasableArrays = new List<ReleasableArray>();
        internal event Action ForceRelese;

        /// <summary>
        /// Позволяет реиспользовать массивне выделяя новую память
        /// </summary>
        public ProArrayBuffer()
        {
        }

        /// <summary>
        /// Очищает буфер
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                ForceRelese?.Invoke();
                ForceRelese = null;
                ReleasableArrays.Clear();
            }
        }
#if DEBUG
        /// <summary>
        /// Выделить массивы указанной длинны
        /// </summary>
        /// <param name="size">размер массива</param>
        /// <param name="lineNumber"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
#else
        /// <summary>
        /// Выделить массивы указанной длинны
        /// </summary>
        /// <param name="size">размер массива</param>
        /// <returns></returns>
#endif
        public IReleasableArray AllocateArray(int size
            #if DEBUG
            ,[CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string caller = null
#endif
            )
        {
            lock (ReleasableArrays)
            {
                for(int i = 0; i < ReleasableArrays.Count; i++)
                {
                    if (ReleasableArrays[i].Bytes.Length>=size)
                    {
                        var arr = ReleasableArrays[i];
                        ReleasableArrays.RemoveAt(i);
                        arr.Allocate(size);
                        return arr;
                    }
                }
                var ra = new ReleasableArray(this, size, false);
#if DEBUG
                ra.DEBUG_NAME = $" at line {lineNumber} ({caller})";
#endif
                return ra;
            }
        }
    }
}