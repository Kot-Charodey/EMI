using System;
using System.Diagnostics;

namespace EMI
{
    internal class TimerBuiltInSync : TimerSync
    {
        private static readonly decimal Freq = (decimal)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        public TimerBuiltInSync(Client client) : base(client)
        {
        }

        public override long Ticks => (long)(Stopwatch.GetTimestamp() * Freq);
    }
}
 