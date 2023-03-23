using System.IO;
using System.Runtime.InteropServices;

namespace EMI.NetStream.Structures
{
    [StructLayout(LayoutKind.Explicit,Size = 12)]
    internal struct SeekInInfo
    {
        [FieldOffset(0)]
        public long offset;
        [FieldOffset(8)]
        public SeekOrigin origin;

        public SeekInInfo(long offset, SeekOrigin origin)
        {
            this.offset = offset;
            this.origin = origin;
        }
    }
}
