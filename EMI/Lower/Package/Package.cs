using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    internal struct Package
    {
        [FieldOffset(0)]
        public ushort RPC_ID;
        [FieldOffset(2)]
        public byte ArgumentCount;
        [FieldOffset(3)]
        public PackageCameBack CameBack;
    }
}
