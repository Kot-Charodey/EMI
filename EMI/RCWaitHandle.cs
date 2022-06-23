using System.Threading;

namespace EMI
{
    using NGC;
    using Indicators;

    internal class RCWaitHandle
    {
        public SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        public AIndicator Indicator;

        public RCWaitHandle(AIndicator indicator)
        {
            Indicator = indicator;
        }
    }
}