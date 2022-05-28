using System;

namespace EMI.SynsInteface
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class OnlyServerAttribute : Attribute
    {

    }
}