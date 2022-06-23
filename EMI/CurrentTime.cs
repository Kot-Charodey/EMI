﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EMI
{
    /// <summary>
    /// Позвояет получить число тиков для расчёта задержек (точное)
    /// </summary>
    internal static class TickTime
    {
        /// <summary>
        /// Текущие кол-во тиков
        /// </summary>
        public static DateTime Now => new DateTime(Stopwatch.GetTimestamp());
    }
}