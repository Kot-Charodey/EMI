using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Lower.Buffer
{
    using Package;
    using Accept;

    /// <summary>
    /// Буффер для сегментированных пакетов (составляет из частей)
    /// </summary>
    internal class PacketAcceptBuffer
    {
        private Dictionary<ulong, ABufferedPackets> Buffer = new Dictionary<ulong, ABufferedPackets>();

        public bool BuildPackage(BitPacketSegmented bitPacket, byte[] data, out PacketInfo packetInfo)
        {
            ABufferedPackets packet = null;

            if (!Buffer.TryGetValue(bitPacket.ID, out packet))
            {
                packet = new BufferedByteArrayPacket(bitPacket);
                Buffer.Add(bitPacket.ID, packet);
            }
            packet.WriteSegment(bitPacket.Segment, data);
            if (packet.IsReady())
            {
                packetInfo = packet.GetPacketInfo();
                return true;
            }
            else
            {
                packetInfo = default;
                return false;
            }
        }

        public bool BuildPackage(BitPacketSegmentedReturned bitPacket, byte[] data, out PacketInfo packetInfo)
        {
            ABufferedPackets packet = null;

            if (!Buffer.TryGetValue(bitPacket.ID, out packet))
            {
                packet = new BufferedByteArrayPacket(bitPacket);
                Buffer.Add(bitPacket.ID, packet);
            }
            packet.WriteSegment(bitPacket.Segment, data);
            if (packet.IsReady())
            {
                packetInfo = packet.GetPacketInfo();
                return true;
            }
            else
            {
                packetInfo = default;
                return false;
            }
        }
    }
}