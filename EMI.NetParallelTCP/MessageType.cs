namespace EMI.NetParallelTCP
{
    /// <summary>
    /// Тип сообщения
    /// </summary>
    internal enum MessageType : byte
    {
        /// <summary>
        /// Информация о пакете
        /// </summary>
        MessageHeader = 100,
        /// <summary>
        /// Часть пакета
        /// </summary>
        MessagePacketSegment = 200
    }
}