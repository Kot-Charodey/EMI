using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EMI.NetParallelTCP
{
    /// <summary>
    /// Заголовок сообщения
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
    internal struct MessageHeader
    {
        public const int SizeOf = 7;
        [FieldOffset(0)]
        public MessageType HeaderType;
        [FieldOffset(1)]
        public ushort ID;
        [FieldOffset(3)]
        private int Size; // если отрицательный то сообщение об ошибке

        public MessageHeader(ushort id, int size, bool isError)
        {
            HeaderType = MessageType.MessageHeader;

            ID = id;
            if (isError)
                Size = -size;
            else
                Size = size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static MessageHeader FromBytes(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                return *(MessageHeader*)ptr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteToBuffer(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
            {
                *((MessageHeader*)ptr) = this;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSize()
        {
            return Math.Abs(Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDisconnectMessage()
        {
            return Size < 0;
        }
    }
}