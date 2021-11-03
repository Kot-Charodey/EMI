using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    /// <summary>
    /// если пакет не дойдёт - тут должна храниться его копия если он понадобиться
    /// </summary>
    internal class SendBackupBuffer
    {
        /// <summary>
        /// Кол во пакетов которые будут храниться в буфере
        /// </summary>
        private const int MaxBufferLength = 5000;

        private struct Data
        {
            public ulong ID;
            public byte[] bytes;

            public Data(ulong iD, byte[] bytes)
            {
                ID = iD;
                this.bytes = bytes;
            }
        }

        private uint BuffI = 0;
        private Data[] Buffer = new Data[MaxBufferLength];

        public void Add(ulong id, byte[] bytes)
        {
            lock (Buffer)
            {
                Buffer[BuffI++] = new Data(id, bytes);
                if (BuffI == MaxBufferLength)
                {
                    BuffI = 0;
                }
            }
        }

        public byte[] Get(ulong id)
        {
            lock (Buffer)
            {
                foreach (Data data in Buffer)
                {
                    if (data.ID == id)
                    {
                        return data.bytes;
                    }
                }
            }
            return null;
        }
    }
}
