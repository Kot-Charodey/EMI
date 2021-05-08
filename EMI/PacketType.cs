namespace EMI
{
    public enum PacketType:byte
    {
        SndStandard,
        SndGuaranteed,
        SndGuaranteedSegmented,
        ReqConnection,
        ReqConnectionGood,
        SndClose,
        ReqGetPkgGuaranted,
        SndGuaranteedReturned,
        SndGuaranteedSegmentedReturned,
        ReqPing0,
        ReqPing1,
    }
}
