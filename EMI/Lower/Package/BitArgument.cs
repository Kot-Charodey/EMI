using System.Runtime.InteropServices;

namespace EMI.Lower.Package
{

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    internal struct BitArgument
    {
        public ushort Size;
    }
}
