using System.Runtime.InteropServices;

namespace EMI.NetStream.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 25)]
    internal struct SeekInfo
    {
        [FieldOffset(0)]
        public bool Result;
        [FieldOffset(1)]
        public long Length;
        [FieldOffset(9)]
        public long Position;
        [FieldOffset(17)]
        public long SeekPosition;

        public SeekInfo(bool result, long length, long position, long seekPosition)
        {
            Result = result;
            Length = length;
            Position = position;
            SeekPosition = seekPosition;
        }
    }
}
