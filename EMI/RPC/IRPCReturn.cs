namespace EMI.RPCInternal
{
    using ProBuffer;

    internal interface IRPCReturn
    {
        int PackSize { get; }
        void PackUp(IReleasableArray array);
    }
}