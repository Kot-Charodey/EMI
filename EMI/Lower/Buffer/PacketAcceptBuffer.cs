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

            lock (Buffer)
            {
                if (!Buffer.TryGetValue(bitPacket.ID, out packet))
                {
                    packet = new BufferedByteArrayPacket(bitPacket);
                    Buffer.Add(bitPacket.ID, packet);
                }
                packet.WriteSegment(bitPacket.Segment, data);
                if (packet.IsReady())
                {
                    packetInfo = packet.GetPacketInfo();
                    Buffer.Remove(bitPacket.ID);
                    return true;
                }
                else
                {
                    packetInfo = default;
                    return false;
                }
            }
        }

        public bool BuildPackage(BitPacketSegmentedReturned bitPacket, byte[] data, out PacketInfo packetInfo)
        {
            ABufferedPackets packet = null;

            lock (Buffer)
            {
                if (!Buffer.TryGetValue(bitPacket.ID, out packet))
                {
                    packet = new BufferedByteArrayPacket(bitPacket);
                    Buffer.Add(bitPacket.ID, packet);
                }
                packet.WriteSegment(bitPacket.Segment, data);
                if (packet.IsReady())
                {
                    packetInfo = packet.GetPacketInfo();
                    Buffer.Remove(bitPacket.ID);
                    return true;
                }
                else
                {
                    packetInfo = default;
                    return false;
                }
            }
        }

        public int[] GetDownloadList(ulong ID, int count)
        {
            lock (Buffer)
            {
                if(Buffer.TryGetValue(ID,out var buffer))
                    return buffer.GetDownloadList(10);
                else
                    return new int[0];
            }
        }
    }
}