using System.Collections.Generic;

namespace EMI
{

    /// <summary>
    /// Хранит сегментные большие пакеты (только до их полной отправки и запроса об окончательном получении)
    /// </summary>
    internal class SendSegmentBackupBuffer
    {
        private Dictionary<ulong, byte[][]> BufferPackages = new Dictionary<ulong, byte[][]>();

        public void Add(ulong ID, byte[][] datas)
        {
            BufferPackages.Add(ID, datas);
        }

        public byte[] Get(ulong ID, ushort Segment)
        {
            if (BufferPackages.TryGetValue(ID, out var datas))
                return datas[Segment];
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
