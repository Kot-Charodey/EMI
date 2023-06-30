using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EMI.Network.NetTCPV3
{
    /// <summary>
    /// Заголовок сообщения
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 2)]
    internal struct MessageHeader
    {
        /// <summary>
        /// Размер структуры <see cref="MessageHeader"/>
        /// </summary>
        public const int SizeOf = 2;
        /// <summary>
        /// Битовая маска - размер пакета
        /// </summary>
        private const uint NoFlagMask = 0b1111111111;
        /// <summary>
        /// Битовая маска - флаги
        /// </summary>
        private const uint FlagMask = 0b0000000000111111;
        [FieldOffset(0)]
        public ushort Size;
        [FieldOffset(1)]
        public PacketHeader Flags;

        public MessageHeader(ushort size, PacketHeader flags)
        {
            Flags = PacketHeader.None;
            Size = size;
            Flags |= flags;
        }
        /// <summary>
        /// Считывает 2 первых байта массива (проверку не делает) и преобразовывает в MessageHeader
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>Заголовок сообщения</returns>
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
        /// <summary>
        /// Получить размер пакета
        /// </summary>
        /// <returns>размер пакета</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetSize()
        {
            return Size & NoFlagMask;
        }
    }
}