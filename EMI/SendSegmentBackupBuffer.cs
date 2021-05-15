using System;
using System.Collections.Generic;

namespace EMI
{

    /// <summary>
    /// Хранит сегментные большие пакеты (только до их полной отправки и запроса об окончательном получении)
    /// </summary>
    internal class SendSegmentBackupBuffer
    {
        private readonly Dictionary<ulong, byte[]> BufferPackages = new Dictionary<ulong, byte[]>();

        public void Add(ulong ID, byte[] data)
        {
            BufferPackages.Add(ID, data);
        }

        public unsafe byte[] Get(ulong ID, ushort Segment)
        {
            if (BufferPackages.TryGetValue(ID, out var data))
            {
                int point = Segment * 1024;
                byte[] seg = new byte[Math.Max(data.Length - point,1024)];
                Array.Copy(data, point, seg, 0, seg.Length);
                return seg;
            }
            else
                return null;
        }

        public void Remove(ulong ID)
        {
            if(BufferPackages.ContainsKey(ID))
                BufferPackages.Remove(ID);
        }
    }
}
