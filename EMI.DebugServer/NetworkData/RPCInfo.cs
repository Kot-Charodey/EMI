using System;
using System.Linq;

namespace EMI.DebugServer.NetworkData
{
    public struct RPCInfo
    {
        public bool IsGlobal;
        public Guid ClientID;

        public RegMethod[] RegisteredMethods;
        public RegMethodForwarding[] RegisteredForwarding;

        public RPCInfo(RPC rpc, bool global, Guid guid)
        {
            IsGlobal = global;
            ClientID = guid;

            var rm = from method in rpc.GetRegisteredMethodsName()
                     select new RegMethod(method.Key, method.Value.Item1, method.Value.Item2);
            RegisteredMethods = rm.ToArray();

            var rmf = from method in rpc.GetRegisteredForwardingName()
                      select new RegMethodForwarding(method.Key, method.Value);
            RegisteredForwarding = rmf.ToArray();
        }

        public static RPCInfo Create()
        {
            return new RPCInfo()
            {
                RegisteredMethods = Array.Empty<RegMethod>(),
                RegisteredForwarding = Array.Empty<RegMethodForwarding>(),
            };
        }
    }
}
