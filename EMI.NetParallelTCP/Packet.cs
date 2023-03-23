using EMI.NGC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.NetParallelTCP
{
    internal class Packet
    {
        public int Offset;
        public int Size;
        public INGCArray Buffer;
    }
}
