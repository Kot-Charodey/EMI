using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetBaseTCP
{
    /// <summary>
    /// Заголовок сообщения
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    internal struct MessageHeader
    {
        /// <summary>
        /// Размер структуры <see cref="MessageHeader"/>
        /// </summary>
        public const int SizeOf = 4;
        /// <summary>
        /// Битовая маска - размер пакета
        /// </summary>
        private const uint NoFlagMask = 0b01111111111111111111111111111111;
        /// <summary>
        /// Битовая маска - флаг
        /// </summary>
        private const uint FlagMask = 0b10000000000000000000000000000000;
        [FieldOffset(0)]
        private uint Data;

        public MessageHeader(int size,bool isMessage)
        {
            Data = ((uint)size & NoFlagMask) | (isMessage ? FlagMask : 0);
        }
        /// <summary>
        /// Считывает 4 первых байта массива (проверку не делает) и преобразовывает в MessageHeader
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
        public int GetSize()
        {
            return (int)(Data & NoFlagMask);
        }
        /// <summary>
        /// Является ли пакет - сообщением об отключении
        /// </summary>
        /// <returns>true - сообщение об отключении</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDisconnectMessage()
        {
            return (Data & FlagMask) > 0;
        }
    }
}