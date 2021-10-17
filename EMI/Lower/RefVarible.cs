using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Lower
{
    /// <summary>
    /// Обёртка для не ссылочных типов, что бы использовать lock
    /// </summary>
    internal class RefVarible<T>
    {
        public T Value;

        public RefVarible(T value)
        {
            Value = value;
        }
    }
}
