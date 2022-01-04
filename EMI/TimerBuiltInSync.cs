using System;
using System.Diagnostics;

namespace EMI
{
    internal class TimerBuiltInSync : TimerSync
    {
        private static double Freq = TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        public override long Ticks => (long)(Stopwatch.GetTimestamp() * Freq);
    }
}
 