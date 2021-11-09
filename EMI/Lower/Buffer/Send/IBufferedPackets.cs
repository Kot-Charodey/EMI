namespace EMI.Lower.Buffer.Send
{
    internal interface IBufferedPackets
    {
        /// <summary>
        /// Является ли пакет сегментным
        /// </summary>
        bool SegmentPacket { get; }

        /// <summary>
        /// Получить пакет или сегмент пакета
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        byte[] GetData(uint segment = 0);
    }
}
