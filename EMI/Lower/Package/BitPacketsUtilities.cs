namespace EMI.Lower.Package
{
    internal static class BitPacketsUtilities
    {
        public static PacketType GetPacketType(this byte[] BitBuffer)
        {
            return (PacketType)(BitBuffer[0]);
        }
    }
}
