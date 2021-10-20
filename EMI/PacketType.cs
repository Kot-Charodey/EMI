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
        ReqConnection0,//client -> server #1
        ReqConnection1,//server -> client #2
        ReqConnection2,//client -> server #3
    }
}
