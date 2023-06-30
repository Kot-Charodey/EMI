using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EMI.Network.NetTCPV3
{
    /// <summary>
    /// Сегментный пакет
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    internal struct MessageSegmentElement
    {
        /// <summary>
        /// Размер структуры <see cref="MessageSegmentElement"/>
        /// </summary>
        public const int SizeOf = 4;
        /// <summary>
        /// Айди пакета
        /// </summary>
        [FieldOffset(0)]
        public uint ID;

        public MessageSegmentElement(uint iD)
        {
            ID = iD;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static MessageSegmentElement FromBytes(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                return *(MessageSegmentElement*)ptr;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteToBuffer(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                *((MessageSegmentElement*)ptr) = this;
            }
        }
    }
}