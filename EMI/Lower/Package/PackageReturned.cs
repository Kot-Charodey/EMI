using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = 9)]
    internal struct PackageReturned
    {
        [FieldOffset(0)]
        public ulong ID;
        [FieldOffset(8)]
        public byte ArgumentCount;

    }
}
