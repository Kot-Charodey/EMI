using System.Runtime.InteropServices;

namespace EMI.NetStream.Structures
{
    [StructLayout(LayoutKind.Explicit,Size = 17)]
    internal struct WriteInfo
    {
        [FieldOffset(0)]
        public bool Result;
        [FieldOffset(1)]
        public long Position;
        [FieldOffset(9)]
        public long Length;

        public WriteInfo(bool result, long position, long length)
        {
            Result = result;
            Position = position;
            Length = length;
        }
    }
}
