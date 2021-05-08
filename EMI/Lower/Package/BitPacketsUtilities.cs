using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Lower.Package
{
    public static class BitPacketsUtilities
    {
        public static PacketType GetPacketType(byte[] BitBuffer)
        {
            return (PacketType)(BitBuffer[0]);
        }

        //public static unsafe ushort GetRPCID(byte[] UnPuckBuffer)
        //{
        //    ushort id;
        //}
    }
}
