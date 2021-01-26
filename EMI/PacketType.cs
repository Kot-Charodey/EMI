namespace EMI
{
    public enum PacketType:byte
    {
        SndStandard,
        SndGuaranteed,
        SndGuaranteedSegmented,
        ReqConnection,
        ReqGoodConnection,
        SndClose,
        ReqGetPkgGuaranted,
        SndNotCreatedPkg,
        ReqPing0,
        ReqPing1,
    }
}
