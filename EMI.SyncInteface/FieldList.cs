using System;
using System.Reflection;

namespace EMI.SyncInterface
{
    internal struct FieldList
    {
        public Type Type;
        public FieldInfo FieldInfo;
        public object Content;
    }
}