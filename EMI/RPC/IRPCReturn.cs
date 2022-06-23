namespace EMI.RPCInternal
{
    using NGC;

    internal interface IRPCReturn
    {
        int PackSize { get; }
        void PackUp(INGCArray array);
    }
}