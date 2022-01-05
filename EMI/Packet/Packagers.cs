using System.Runtime.InteropServices;
using SmartPackager;

namespace EMI.Packet
{
    internal static class Packagers
    {

        public static Packager.M<PacketHeader> PacketHeader = Packager.Create<PacketHeader>();

        //====================================Ping====================================//

        public static Packager.M<PacketHeader, long> Ping = Packager.Create<PacketHeader, long>(); 
        public static int PingSizeOf = (int)Ping.CalcNeedSize(default, default);

        //====================================TimeSyns====================================//

        public static Packager.M<PacketHeader,long> Ticks = Packager.Create<PacketHeader,long>();
        public static Packager.M<PacketHeader,ushort> Integ = Packager.Create<PacketHeader,ushort>();
        public static int TicksSizeOf = (int)Ticks.CalcNeedSize(default, default);
        public static int IntegSizeOf = (int)Integ.CalcNeedSize(default, default);

        //====================================RegisterMethod====================================//

        public static Packager.M<PacketHeader, ushort> RegisterMethodRemove = Packager.Create<PacketHeader, ushort>();
        public static Packager.M<PacketHeader, ushort, string> RegisterMethodAdd = Packager.Create<PacketHeader, ushort, string>();
        public static Packager.M<PacketHeader, ushort[], string[]> RegisterMethodList = Packager.Create<PacketHeader, ushort[], string[]>();

        public static int RegisterMethodRemoveSizeOf = (int)RegisterMethodRemove.CalcNeedSize(default, default);

        //====================================RPC====================================//

        // ID,Ticks ... [array data]
        public static Packager.M<PacketHeader, ushort, long> RPC = Packager.Create<PacketHeader, ushort, long>();
        // ID,RPCReturnStatus ... [array data]
        public static Packager.M<PacketHeader, ushort, RPCReturnStatus> RPCAnswer = Packager.Create<PacketHeader, ushort, RPCReturnStatus>();
        public static int RPCSizeOf = (int)RPC.CalcNeedSize(default, default, default);
        public static int RPCAnswerSizeOf = (int)RPCAnswer.CalcNeedSize(default, default, default);
    }

}
