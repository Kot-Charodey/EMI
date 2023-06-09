﻿using System;

namespace EMI.SyncInterface
{
    using EMI;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class RCTypeOptionAttribute : Attribute
    {
        public RCType RCType;
        public RCTypeOptionAttribute(RCType type)
        {
            RCType = type;
        }
    }
}