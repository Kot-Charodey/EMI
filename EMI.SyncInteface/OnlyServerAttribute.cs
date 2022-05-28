using System;

namespace EMI.SyncInterface
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class OnlyServerAttribute : Attribute
    {

    }
}