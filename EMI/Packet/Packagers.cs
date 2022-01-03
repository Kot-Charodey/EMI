using System.Runtime.InteropServices;
using SmartPackager;

namespace EMI.Packet
{
    internal static class Packagers
    {
        public static Packager.M<PacketHeader> PPacketHeader = Packager.Create<PacketHeader>();

        public static Packager.M<Tics> PTics = Packager.Create<Tics>();
        public static Packager.M<Integ> PInteg = Packager.Create<Integ>();


        public static Packager.M<long> PLong = Packager.Create<long>();
        public static Packager.M<ushort> PUshort = Packager.Create<ushort>();

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
        public struct Tics
        {
            public const int SizeOf = PacketHeader.SizeOf + sizeof(long);

            [FieldOffset(0)]
            public PacketHeader PacketHeader;
            [FieldOffset(PacketHeader.SizeOf)]
            public long Tiks;
        }
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
        public struct Integ
        {
            public const int SizeOf = PacketHeader.SizeOf + sizeof(ushort);

            [FieldOffset(0)]
            public PacketHeader PacketHeader;
            [FieldOffset(PacketHeader.SizeOf)]
            public ushort Integration;
        }
    }

}
