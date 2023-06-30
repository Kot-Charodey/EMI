using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EMI.Network.NetTCPV3
{
    /// <summary>
    /// Заголовок сегментного пакета
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
    internal struct MessageSegmentHeader
    {
        /// <summary>
        /// Размер структуры <see cref="MessageSegmentHeader"/>
        /// </summary>
        public const int SizeOf = 8;
        /// <summary>
        /// Айди пакета
        /// </summary>
        [FieldOffset(0)]
        public uint ID;
        /// <summary>
        /// Размер пакета
        /// </summary>
        [FieldOffset(4)]
        public int Size;

        public MessageSegmentHeader(uint iD, int size)
        {
            ID = iD;
            Size = size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static MessageSegmentHeader FromBytes(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                return *(MessageSegmentHeader*)ptr;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteToBuffer(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                *((MessageSegmentHeader*)ptr) = this;
            }
        }
    }
}