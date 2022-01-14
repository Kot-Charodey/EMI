using SmartPackager;

namespace EMI.Lower.Package
{
    internal static class Packagers
    {
        //SmartPackager - создаёт упаковщики для дальнейшего использования
        public static readonly Packager.M<BitPacketReqGetPkgSegmented, int[]> PkgSegmented = Packager.Create<BitPacketReqGetPkgSegmented, int[]>();
        public static readonly Packager.M<PacketType, ulong[]> PacketGetPkg = Packager.Create<PacketType, ulong[]>();
        public static readonly Packager.M<BitPacketSegmented, byte[]> Segmented = Packager.Create<BitPacketSegmented, byte[]>();
        public static readonly Packager.M<BitPacketSegmentedReturned, byte[]> SegmentedReturned = Packager.Create<BitPacketSegmentedReturned, byte[]>();
        public static readonly Packager.M<BitPacketSimple> SimpleNoData = Packager.Create<BitPacketSimple>();
        public static readonly Packager.M<BitPacketSimple, byte[]> Simple = Packager.Create<BitPacketSimple, byte[]>();
        public static readonly Packager.M<BitPacketGuaranteed> GuaranteedNoData = Packager.Create<BitPacketGuaranteed>();
        public static readonly Packager.M<BitPacketGuaranteed, byte[]> Guaranteed = Packager.Create<BitPacketGuaranteed, byte[]>();
        public static readonly Packager.M<BitPacketGuaranteedReturned> GuaranteedReturnedNoData = Packager.Create<BitPacketGuaranteedReturned>();
        public static readonly Packager.M<BitPacketGuaranteedReturned, byte[]> GuaranteedReturned = Packager.Create<BitPacketGuaranteedReturned, byte[]>();
        public static readonly Packager.M<BitPackageDeliveryСompleted> SndDeliveryСompletedPackage = Packager.Create<BitPackageDeliveryСompleted>();

    }
}
