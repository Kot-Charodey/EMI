namespace EMI
{
    internal enum PacketType:byte
    {
        SndClose,
        SndSimple,
        SndGuaranteed,
        SndGuaranteedSegmented,
        SndGuaranteedReturned,
        SndGuaranteedSegmentedReturned,
        SndFullyReceivedSegmentPackage,
        ReqGetPkg,
        ReqPing0,
        ReqPing1,
        ReqConnection0,
        ReqConnection1,
    }
}
