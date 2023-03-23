using System.Runtime.InteropServices;

namespace EMI.NetStream.Structures
{
    [StructLayout(LayoutKind.Explicit,Size = 17)]
    internal struct FlushInfo
    {
        [FieldOffset(0)]
        public bool Result;
        [FieldOffset(1)]
        public long Length;
        [FieldOffset(9)]
        public long Position;

        public FlushInfo(bool result, long length, long position)
        {
            Result = result;
            Length = length;
            Position = position;
        }
    }
}
