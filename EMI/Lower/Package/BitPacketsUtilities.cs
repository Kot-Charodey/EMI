﻿namespace EMI.Lower.Package
{
    internal static class BitPacketsUtilities
    {
        public static PacketType GetPacketType(this byte[] BitBuffer)
        {
            return (PacketType)(BitBuffer[0]);
        }

        /// <summary>
        /// Рассчитать на сколько надо поделить большой пакет
        /// </summary>
        /// <param name="size">размер данных</param>
        /// <returns></returns>
        public static ushort CalcSegmentCount(int size)
        {
            ushort count = (ushort)(size / 1024);
            if (size % 1024 > 0) //если size не делиться нацело округлить в верхнию сторону
                count++;
            return count;
        }

        /// <summary>
        /// Рассчитать на сколько надо поделить большой пакет
        /// </summary>
        /// <param name="size">размер данных</param>
        /// <returns></returns>
        public static ushort CalcSegmentCount(long size)
        {
            ushort count = (ushort)(size / 1024);
            if (size % 1024 > 0) //если size не делиться нацело округлить в верхнию сторону
                count++;
            return count;
        }
    }
}
