using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EMI
{
    /// <summary>
    /// Позвояет получить текущее время (оптимизирован для частых запросов)   (DateTime.UtcNow слишком долго)
    /// </summary>
    public static class CurrentTime
    {
        /// <summary>
        /// Текущее время
        /// </summary>
        public static DateTime Now => new DateTime(TimerPoint + Stopwatch.Elapsed.Ticks);
        private static long TimerPoint;
        private readonly static Stopwatch Stopwatch;

        static CurrentTime()
        {
            Stopwatch = new Stopwatch();
            TimerPoint = DateTime.UtcNow.Ticks;
            Stopwatch.Start();

            //синхронизирует время с DateTime
            Timer timer = new Timer((_) =>
              {
                  TimerPoint += DateTime.UtcNow.Ticks - Now.Ticks;
              }, null, 0, 60000);
        }
    }
}