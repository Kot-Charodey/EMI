using System.Threading;

namespace EMI
{
    using ProBuffer;

    internal class RCWaitHandle
    {
        public SemaphoreSlim Semaphore = new SemaphoreSlim(0,1);
        public IReleasableArray Array;
    }
}