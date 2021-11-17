using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using EMI;

namespace StressTest
{
    public static class StandLogClient
    {
        public static void Type1(Client client, long workCounter,bool error, Stopwatch stopwatch)
        {
            return;
            List<string> lines = new List<string>();

            double work = workCounter / stopwatch.Elapsed.TotalSeconds;
            if (error)
            {
                lines.Add("===============Внимание битые данные===============");
                lines.Add("");
                lines.Add("");
            }

            double datPerSec = work * 5001 * sizeof(int);
            double datAll = workCounter * 5001 * sizeof(int);

            lines.Add($"Ping: {client.PingMS}");
            lines.Add($"Запросов в секунду: {work}");
            lines.Add($"Всего запросов: {workCounter}");
            lines.Add($"Отправленно полезных данных за секунду {MegaSize.ForByte(datPerSec)}  [{MegaSize.ForBit(datPerSec)}]");
            lines.Add($"Всего полезных данных {MegaSize.ForByte(datAll)}  [{MegaSize.ForBit(datAll)}]");

            for (int l = 0; l < lines.Count; l++)
            {
                for (int i = lines[l].Length; i < Console.WindowWidth - 1; i++)
                {
                    lines[l] += " ";
                }
            }
            string str = "";
            foreach (var line in lines)
            {
                str += line + "\n";
            }
            Console.SetCursorPosition(0, 0);
            Console.Write(str);
            Thread.Sleep(10);
        }
    }
}
