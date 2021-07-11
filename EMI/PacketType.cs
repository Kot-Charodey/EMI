namespace EMI
{
    internal enum PacketType:byte
    {
        SndClose,
        SndSimple,
        SndGuaranteed,
        SndGuaranteedRtr,//результат данного пакета следует вернуть
        SndGuaranteedSegmented,
        SndGuaranteedRtrSegmented,//требует вернуть результат
        SndGuaranteedReturned,
        SndGuaranteedSegmentedReturned,
        SndFullyReceivedSegmentPackage,//пакет доставлен
        ReqGetPkg,
        ReqGetPkgSegmented,
        ReqPing0,
        ReqPing1,
        ReqConnection0,
        ReqConnection1,
    }
}
