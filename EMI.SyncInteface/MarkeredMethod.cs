using System.Reflection;

namespace EMI.SyncInterface
{
    internal struct MarkeredMethod
    {
        public MethodInfo MethodInfo;
        public object Indicator;

        public MarkeredMethod(MethodInfo methodInfo, object indicator)
        {
            MethodInfo = methodInfo;
            Indicator = indicator;
        }
    }
}