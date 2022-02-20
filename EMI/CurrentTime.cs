using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    /// <summary>
    /// Позвояет получить текущее время (оптимизирован для частых запросов)
    /// </summary>
    public static class CurrentTime
    {
        /// <summary>
        /// Текущее время
        /// </summary>
        public static DateTime Now { get; private set; }
        private static int TiksCount = 0;

        static CurrentTime()
        {
            Now = DateTime.UtcNow;
            
            Timer timer = new Timer((_) =>
              {
                  Now.AddMilliseconds(5);
                  TiksCount++;
                  if (TiksCount >= 60000)//кажется Timer не насколько точный что бы ему доверять - мы будем синхронизировать, иногда
                  {
                      TiksCount = 0;
                      Now = DateTime.UtcNow;
                  }
              }, null, 0, 5);
        }
    }
}
