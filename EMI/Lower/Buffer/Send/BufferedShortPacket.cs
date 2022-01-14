using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Lower.Buffer.Send
{
    /// <summary>
    /// Буффер для маленького или не сегментированных пакета
    /// </summary>
    internal class BufferedShortPacket : IBufferedPackets
    {
        public bool SegmentPacket => false;
        private byte[] data;

        public BufferedShortPacket(byte[] data)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public byte[] GetData(int segment = 0)
        {
            return data;
        }
    }
}
