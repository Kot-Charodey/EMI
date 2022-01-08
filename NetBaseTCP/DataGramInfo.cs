using System.Linq;
using System.Runtime.InteropServices;

namespace NetBaseTCP
{

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    internal struct DataGramInfo
    {
        public const int SizeOf = 4;
        public const uint NoFlagMask = 0b01111111111111111111111111111111;
        public const uint FlagMask = 0b10000000000000000000000000000000;
        [FieldOffset(0)]
        public uint Data;

        public DataGramInfo(int size,bool isMessage)
        {
            Data = ((uint)size & NoFlagMask) | (isMessage ? FlagMask : 0);
        }

        public int GetSize()
        {
            return (int)(Data & NoFlagMask);
        }

        public bool GetIsDisconnectMessage()
        {
            return (Data & FlagMask) > 0;
        }
    }
}