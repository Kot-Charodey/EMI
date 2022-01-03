using System.Diagnostics;

namespace EMI
{
    internal class TimerBuiltInSync : TimerSync
    {
        public override long Ticks => Stopwatch.GetTimestamp() / Stopwatch.Frequency;
    }
}
