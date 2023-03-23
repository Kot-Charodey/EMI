using System;

namespace EMI.NetStream.Structures
{
    internal struct ReadInfo
    {
        public bool Result;
        public int ReadLen;
        public byte[] Buffer;
        public long Lenght;
        public long Position;

        public ReadInfo(bool result, int readLen, byte[] buffer, long lenght, long position)
        {
            Result = result;
            ReadLen = readLen;
            Buffer = buffer;
            Lenght = lenght;
            Position = position;
        }
    }
}
