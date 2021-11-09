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
    public class Test_BufferSend
    {
        Random Random = new Random();
        
        [TestMethod]
        public void Test1_PacketSendBuffer_ShortPacket()
        {
            PacketSendBuffer buffer = new PacketSendBuffer();
            BitPacketGuaranteed bitPacket = new BitPacketGuaranteed()
            {
                ID = 1,
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = 404,
            };
            byte[] data = new byte[500];
            Random.NextBytes(data);

            byte[] AllData = Packagers.Guaranteed.PackUP(bitPacket, data);

            Assert.IsNull(buffer.GetPacket(1));
            buffer.Storing(1, AllData).Wait();

            Assert.IsNotNull(buffer.GetPacket(1));
            CollectionAssert.AreEqual(buffer.GetPacket(1),AllData);

            buffer.RemovePacket(1);
            Assert.IsNull(buffer.GetPacket(1));
        }

        [TestMethod]
        public void Test2_PacketSendBuffer_Lock()
        {
            PacketSendBuffer buffer = new PacketSendBuffer();
            byte[][] TestData = new byte[PacketSendBuffer.Capacity+1][];
            for (ulong i = 0; i < PacketSendBuffer.Capacity; i++)
            {
                BitPacketGuaranteed bitPacket = new BitPacketGuaranteed()
                {
                    ID = i,
                    PacketType = PacketType.SndGuaranteed,
                    RPCAddres = 404,
                };
                byte[] data = new byte[Random.Next(100, 1025)];
                Random.NextBytes(data);
                TestData[i] = Packagers.Guaranteed.PackUP(bitPacket, data);

                buffer.Storing(i, TestData[i]).Wait();
            }

            var tas = TaskUtilities.InvokeAsync(() =>
            {
                ulong i = PacketSendBuffer.Capacity;
                BitPacketGuaranteed bitPacket = new BitPacketGuaranteed()
                {
                    ID = i+2,
                    PacketType = PacketType.SndGuaranteed,
                    RPCAddres = 404,
                };
                byte[] data = new byte[Random.Next(100, 1025)];
                Random.NextBytes(data);
                TestData[i] = Packagers.Guaranteed.PackUP(bitPacket, data);

                buffer.Storing(i+2, TestData[i]).Wait();
            },new System.Threading.CancellationTokenSource(500));

            Assert.IsFalse(tas.Result,"Проверка - заблокировался ли поток");
            buffer.RemovePacket(2);

            var token = new System.Threading.CancellationTokenSource();

            var tas2 = TaskUtilities.InvokeAsync(() =>
            {
                buffer.Storing(PacketSendBuffer.Capacity, TestData[PacketSendBuffer.Capacity]).Wait();
            }, token);
            buffer.RemovePacket(1);
            token.CancelAfter(500);
            Assert.IsTrue(tas2.Result, "Проверка - снялась ли блокировка потока после вызова  buffer.RemovePacket(0);");
        }

        [TestMethod]
        public void Test3_PacketSendBuffer_LongPacket()
        {
            PacketSendBuffer buffer = new PacketSendBuffer();

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

            bool crash = false;
            var crashData=Packagers.Segmented.PackUP(bitPacket, Arrs[0]);
            try
            {
                buffer.Storing(0, crashData).Wait();
            }
            catch
            {
                crash = true;
            }

            Assert.IsTrue(crash);

            buffer.Storing(0, bitPacket, RawData).Wait();

            for (int i = 0; i < 3; i++)
            {
                byte[] get = buffer.GetPacket(0, i);
                Packagers.Segmented.UnPack(get, 0, out var t1, out var t2);
                CollectionAssert.AreEqual(Arrs[i], t2);
                bitPacket.Segment = i;
                Assert.AreEqual(t1, bitPacket);
            }
        }


        [TestMethod]
        public void Test4_PacketSendBuffer_2GBPacket()
        {
            PacketSendBuffer buffer = new PacketSendBuffer();

            List<byte[]> Arrs = new List<byte[]>();
            for(int i=0;i< 2000000000/1024; i++)
            {
                Arrs.Add(new byte[1024]);
                Random.NextBytes(Arrs[i]);
            }
            List<byte> RawDataL = new List<byte>();
            for (int i = 0; i < Arrs.Count; i++)
            {
                RawDataL.AddRange(Arrs[i].ToArray());
            }
            byte[] RawData = RawDataL.ToArray();

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            BitPacketSegmented bitPacket = new BitPacketSegmented()
            {
                ID = 0,
                PacketType = PacketType.SndGuaranteedSegmented,
                RPCAddres = 404,
                Segment = 0,
                SegmentCount = Arrs.Count,
            };

            buffer.Storing(0, bitPacket, RawData).Wait();

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine("Storing time:" + stopwatch.Elapsed.TotalSeconds);

            for (int i = 0; i < 30; i++) //слишком долго всё проверять
            {
                byte[] get = buffer.GetPacket(0, i);
                Packagers.Segmented.UnPack(get, 0, out var t1, out var t2);
                CollectionAssert.AreEqual(Arrs[i], t2);
                bitPacket.Segment = i;
                Assert.AreEqual(t1, bitPacket);
            }
        }
    }
}
