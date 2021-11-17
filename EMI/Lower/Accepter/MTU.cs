using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Lower.Accepter
{
    internal static class MTU
    {
        /// <summary>
        /// Сколько можно отправить данных по сети в одном пакете
        /// </summary>
        public const int Size = 1248;
    }
}
