using System;

namespace EMI.SynsInteface
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class OnlyClientAttribute : Attribute
    {

    }
}