using SmartPackager;

namespace EMI.RPCInternal
{
    using ProBuffer;

    internal struct RPCReturn<T> : IRPCReturn
    {
        private Packager.M<T> Pack;
        private T Data;
        public int PackSize { get; private set; }

        public static RPCReturn<T> Create()
        {
            RPCReturn<T> s = new RPCReturn<T>
            {
                Pack = Packager.Create<T>(),
                Data = default,
                PackSize = 0
            };

            return s;
        }

        public void Set(T data)
        {
            Data = data;
            PackSize = (int)Pack.CalcNeedSize(data);
        }

        public void PackUp(IReleasableArray array)
        {
            Pack.PackUP(array.Bytes, array.Offset, Data);
        }
    }
}