using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MSTests
{
    using EMI;
    using EMI.Lower;
    using EMI.Lower.Package;
    using EMI.Lower.Buffer;
    using System.Collections.Generic;

    [TestClass]
    public class Test_BufferAccept
    {
        Random Random = new Random();
        
        [TestMethod]
        public void Test1_PacketAcceptBuffer()
        {
            PacketAcceptBuffer buffer = new PacketAcceptBuffer();

            BitPacketSegmented bitPacket = new BitPacketSegmented()
            {
                ID = 0,
                PacketType = PacketType.SndGuaranteedSegmented,
                RPCAddres = 404,
                Segment = 0,
                SegmentCount = 3,
            };
            List<byte[]> Arrs = new List<byte[]>();
            Arrs.Add(new byte[1024]);
            Arrs.Add(new byte[1024]);
            Arrs.Add(new byte[300]);
            Random.NextBytes(Arrs[0]);
            Random.NextBytes(Arrs[1]);
            Random.NextBytes(Arrs[2]);

            List<byte> RawDataL = new List<byte>();
            RawDataL.AddRange(Arrs[0].ToArray());
            RawDataL.AddRange(Arrs[1].ToArray());
            RawDataL.AddRange(Arrs[2].ToArray());

            byte[] RawData = RawDataL.ToArray();

            bool a=false;
            bool b=false;
            bool c=false;

            while (true)
            {
                bitPacket.Segment = Random.Next(0, 3);
                if(buffer.BuildPackage(bitPacket, Arrs[bitPacket.Segment], out var inf))
                {
                    Assert.AreEqual(inf.DataLength, RawData.Length);
                    for(int i = 0; i < RawData.Length; i++)
                    {
                        Assert.AreEqual(inf.Data[i], RawData[i]);
                    }
                    break;
                }

                if (bitPacket.Segment == 0) a = true;
                if (bitPacket.Segment == 1) b = true;
                if (bitPacket.Segment == 2) c = true;
                if (a && b && c)
                {
                    Assert.Fail("Все пакеты доставлены но цил не сборки не закончен");
                }
            }
        }
    }
}
