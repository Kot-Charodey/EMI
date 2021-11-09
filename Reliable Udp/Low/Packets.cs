using System;
using SmartPackager;

namespace Reliable_Udp.Low
{
    internal static class Packets
    {
        public static readonly Packager.M<PacketType> Close = Packager.Create<PacketType>();
        public static readonly Packager.M<PacketType> СonnectionRequest = Packager.Create<PacketType>();
        public static readonly Packager.M<PacketType,int> ConnectionTest = Packager.Create<PacketType,int>();
        public static readonly Packager.M<PacketType,int> ConnectionTestResult = Packager.Create<PacketType,int>();
        public static readonly Packager.M<PacketType,int> ConnectionCompleate = Packager.Create<PacketType,int>();
        public static readonly Packager.M<PacketType, DateTime> Ping0 = Packager.Create<PacketType, DateTime>();
        public static readonly Packager.M<PacketType, DateTime> Ping1 = Packager.Create<PacketType, DateTime>();
        public static readonly Packager.M<PacketType,ulong[]> RequestPackage = Packager.Create<PacketType,ulong[]>();
        public static readonly Packager.M<PacketType> PackageDelivered = Packager.Create<PacketType>();
        public static readonly Packager.M<PacketType> SndSimple = Packager.Create<PacketType>();
        public static readonly Packager.M<PacketType, ulong[]> SndGuaranteed = Packager.Create<PacketType, ulong[]>();
        public static readonly Packager.M<PacketType, ulong[]> SndGuaranteedOrder = Packager.Create<PacketType, ulong[]>();
        public static readonly Packager.M<PacketType, ulong, int, int> SndGuaranteedSegment = Packager.Create<PacketType, ulong, int, int>();
        public static readonly Packager.M<PacketType, ulong, int, int> SndGuaranteedOrderSegment = Packager.Create<PacketType, ulong, int, int>();
   
        public static PacketType GetPacketType(this byte[] data)
        {
            return (PacketType)data[0];
        }
    }
}