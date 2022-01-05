﻿using System.Runtime.InteropServices;
using SmartPackager;

namespace EMI.Packet
{
    internal static class Packagers
    {

        public static Packager.M<PacketHeader> PacketHeader = Packager.Create<PacketHeader>();

        public static Packager.M<PacketHeader, long> Ping = Packager.Create<PacketHeader, long>(); 
        public static int PingSizeOf = (int)Ping.CalcNeedSize(default, default);

        public static Packager.M<PacketHeader,long> Ticks = Packager.Create<PacketHeader,long>();
        public static Packager.M<PacketHeader,ushort> Integ = Packager.Create<PacketHeader,ushort>();
        public static int TicksSizeOf = (int)Ticks.CalcNeedSize(default, default);
        public static int IntegSizeOf = (int)Integ.CalcNeedSize(default, default);

        public static Packager.M<PacketHeader, ushort> RegisterMethodRemove = Packager.Create<PacketHeader, ushort>();
        public static Packager.M<PacketHeader, ushort, string> RegisterMethodAdd = Packager.Create<PacketHeader, ushort, string>();
        public static Packager.M<PacketHeader, ushort[], string[]> RegisterMethodList = Packager.Create<PacketHeader, ushort[], string[]>();

        public static int RegisterMethodRemoveSizeOf = (int)RegisterMethodRemove.CalcNeedSize(default, default);

        /// <summary>
        /// ID,Ticks ... [array data]
        /// </summary>
        public static Packager.M<PacketHeader, ushort, long> RPC = Packager.Create<PacketHeader, ushort, long>();
        /// <summary>
        /// ID ... [array data]
        /// </summary>
        public static Packager.M<PacketHeader, ushort> RPCAnswer = Packager.Create<PacketHeader, ushort>();
        public static int RPCSizeOf = (int)RPC.CalcNeedSize(default, default, default);
        public static int RPCAnswerSizeOf = (int)RPCAnswer.CalcNeedSize(default, default);
    }

}
