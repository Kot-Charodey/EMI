using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EMI.NetParallelTCP
{
    /// <summary>
    /// Сегмент пакета
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
    internal struct MessagePacketSegment
    {
        public const int SizeOf = 3;

        [FieldOffset(0)]
        public MessageType HeaderType;
        [FieldOffset(1)]
        public ushort ID;

        public MessagePacketSegment(ushort id)
        {
            HeaderType = MessageType.MessagePacketSegment;
            ID = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static MessagePacketSegment FromBytes(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                return *(MessagePacketSegment*)ptr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteToBuffer(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                *((MessagePacketSegment*)ptr) = this;
            }
        }
    }
}