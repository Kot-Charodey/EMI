using System.IO;
using System.Runtime.InteropServices;

namespace EMI.NetStream.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 3)]
    internal struct NetStreamInfo
    {
        [FieldOffset(0)]
        public bool CanRead;
        [FieldOffset(1)]
        public bool CanSeek;
        [FieldOffset(2)]
        public bool CanWrite;

        public NetStreamInfo(Stream stream)
        {
            CanRead = stream.CanRead;
            CanSeek = stream.CanSeek;
            CanWrite = stream.CanWrite;
        }
    }
}
