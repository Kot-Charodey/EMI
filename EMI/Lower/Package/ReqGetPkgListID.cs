using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ReqGetPkgListID
    {
        [FieldOffset(0)]
        public byte CountID;
        [FieldOffset(1)]
        public fixed ulong IDData[100];

        public unsafe ushort GetSize()
        {
            return (ushort)(CountID * sizeof(long) + 1);
        }

        public ulong[] GetListID()
        {
            ulong[] arr = new ulong[CountID];

            for(int i = 0; i < CountID; i++)
            {
                arr[i] = IDData[i];
            }

            return arr;
        }
    }
}
