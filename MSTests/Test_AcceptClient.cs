using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace MSTests
{
    using SmartPackager;

    using EMI;
    using EMI.Lower;
    using EMI.Lower.Package;
    using EMI.Lower.Buffer;
    using EMI.Lower.Accepter;

    [TestClass]
    public class Test_AcceptClient
    {
        private const int Wait = 50;
        Random Random = new Random();

        [TestMethod]
        public void Test1_Simple()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            string dataTest = "";
            bool test = false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddress<string> m1 = new RPCAddress<string>(rat);
            client.RPC.RegisterMethod(m1, 0, (db) => { test = true; dataTest = db; });

            string sendData = "Boba";
            var pac = Packager.Create<string>();

            byte[] buf = Packagers.Simple.PackUP(new BitPacketSimple() { PacketType = PacketType.SndSimple, RPCAddres = m1.ID }, pac.PackUP(sendData));
            accepter.Invoke(new AcceptData(buf.Length, buf));

            Task.Delay(Wait).Wait();

            Assert.IsTrue(test);
            CollectionAssert.AreEqual(sendData.ToCharArray(), dataTest.ToCharArray());
        }

        [TestMethod]
        public void Test2_SimpleNoData()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            bool test=false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddress m1 = new RPCAddress(rat);
            client.RPC.RegisterMethod(m1, 0, () => { test = true; });

            byte[] buf = Packagers.SimpleNoData.PackUP(new BitPacketSimple() { PacketType = PacketType.SndSimple, RPCAddres = m1.ID });
            accepter.Invoke(new AcceptData(buf.Length, buf));

            Task.Delay(Wait).Wait();

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void Test3_Guranted()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            string dataTest = "";
            bool test = false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddress<string> m1 = new RPCAddress<string>(rat);
            client.RPC.RegisterMethod(m1, 0, (db) => { test = true; dataTest = db; });

            string sendData = "Boba";
            var pac = Packager.Create<string>();

            byte[] buf = Packagers.Guaranteed.PackUP(new BitPacketGuaranteed() { PacketType = PacketType.SndGuaranteed, RPCAddres = m1.ID, ID = 0 }, pac.PackUP(sendData));
            accepter.Invoke(new AcceptData(buf.Length, buf));

            Task.Delay(Wait).Wait();


            Debug.WriteLine("IMyAccepter->");
            foreach (var snd in acp.sndDats)
                Debug.WriteLine($"{snd.PacketType}  size:({snd.count})");


            Assert.IsTrue(test);
            if (acp.sndDats.Count == 1)
                Assert.IsTrue(acp.sndDats[0].PacketType == PacketType.SndDeliveryСompletedPackage);
            else
                Assert.Fail("Ожидались другие пакеты!");
            CollectionAssert.AreEqual(sendData.ToCharArray(), dataTest.ToCharArray());
        }

        [TestMethod]
        public void Test4_GurantedNoData()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            bool test = false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddress m1 = new RPCAddress(rat);
            client.RPC.RegisterMethod(m1, 0, () => { test = true;});

            var pac = Packager.Create<string>();

            byte[] buf = Packagers.GuaranteedNoData.PackUP(new BitPacketGuaranteed() { PacketType = PacketType.SndGuaranteed, RPCAddres = m1.ID, ID = 0 });
            accepter.Invoke(new AcceptData(buf.Length, buf));

            Task.Delay(Wait).Wait();


            Debug.WriteLine("IMyAccepter->");
            foreach (var snd in acp.sndDats)
                Debug.WriteLine($"{snd.PacketType}  size:({snd.count})");


            Assert.IsTrue(test);
            if (acp.sndDats.Count == 1)
                Assert.IsTrue(acp.sndDats[0].PacketType == PacketType.SndDeliveryСompletedPackage);
            else
                Assert.Fail("Ожидались другие пакеты!");
        }

        [TestMethod]
        public void Test5_GuaranteedRtr()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            string dataTest = "";
            bool test = false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddressOut<int,string> m1 = new RPCAddressOut<int,string>(rat);
            client.RPC.RegisterMethod(m1, 0, (db) => { test = true; dataTest = db; return db.GetHashCode(); });

            string sendData = "Boba";
            var pac = Packager.Create<string>();

            byte[] buf = Packagers.Guaranteed.PackUP(new BitPacketGuaranteed() { PacketType = PacketType.SndGuaranteedRtr, RPCAddres = m1.ID, ID = 0 }, pac.PackUP(sendData));
            accepter.Invoke(new AcceptData(buf.Length, buf));

            Task.Delay(Wait).Wait();

            Debug.WriteLine("IMyAccepter->");
            foreach (var snd in acp.sndDats)
                Debug.WriteLine($"{snd.PacketType}  size:({snd.count})");


            Assert.IsTrue(test);
            if (acp.sndDats.Count == 2)
            {
                Assert.IsTrue(acp.sndDats[0].PacketType == PacketType.SndDeliveryСompletedPackage);
                var pac1 = Packager.Create<int>();
                Packagers.GuaranteedReturned.UnPack(acp.sndDats[1].Data, 0, out var t1, out var t2);
                pac1.UnPack(t2, 0, out var t3);
                Assert.IsTrue(t3 == sendData.GetHashCode());
            }
            else
                Assert.Fail("Ожидались другие пакеты!");
            CollectionAssert.AreEqual(sendData.ToCharArray(), dataTest.ToCharArray());
        }

        [TestMethod]
        public void Test6_GuaranteedSegmentedRtr()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            byte[] sendData = new byte[1500];
            Random.NextBytes(sendData);
            bool test = false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddressOut<int,byte[]> m1 = new RPCAddressOut<int,byte[]>(rat);
            client.RPC.RegisterMethod(m1, 0, (db) =>
            {
                test = true;
                return db.Length;
            });

            var task = client.RemoteGuaranteedExecution(m1, sendData);
            Task.Delay(Wait).Wait();

            byte[] bb2 = Client._GetPacketSendBuffer(client).GetPacket(0, 1);
            accepter.Invoke(new AcceptData(bb2.Length, bb2));
            accepter.Invoke(new AcceptData(acp.sndDats[0].count, acp.sndDats[0].Data));
            Task.Delay(Wait).Wait();
            accepter.Invoke(new AcceptData(acp.sndDats[2].count, acp.sndDats[2].Data));
            Task.Delay(Wait).Wait();

            Debug.WriteLine("IMyAccepter->");
            foreach (var snd in acp.sndDats)
                Debug.WriteLine($"{snd.PacketType}  size:({snd.count})");



            if (task.IsCompleted)
            {
                Assert.IsTrue(sendData.Length == task.Result);
            }
            else
            {
                Assert.Fail("Задача не завершилась");
            }

            Assert.IsTrue(test);
            if (acp.sndDats.Count == 4)
            {
                Assert.IsTrue(acp.sndDats[0].PacketType == PacketType.SndGuaranteedRtrSegmented);
                Assert.IsTrue(acp.sndDats[1].PacketType == PacketType.SndDeliveryСompletedPackage);
                Assert.IsTrue(acp.sndDats[2].PacketType == PacketType.SndGuaranteedReturned);
                Assert.IsTrue(acp.sndDats[3].PacketType == PacketType.SndDeliveryСompletedPackage);
            }
            else
                Assert.Fail("Ожидались другие пакеты!");
            //CollectionAssert.AreEqual(sendData.ToCharArray(), dataTest.ToCharArray());
        }


        /// <summary>
        /// Тест отправки большого пакета с большим возвращаймым результатом
        /// </summary>
        [TestMethod]
        public void Test7_GuaranteedSegmentedBigRtr()
        {
            Client client = Client._GetClientForTest();
            Acp acp = new Acp();
            var accepter = Client._InitAndGetProcessAccept(client, acp);

            byte[] sendData = new byte[1500];
            Random.NextBytes(sendData);
            bool test = false;

            RPCAddressTable rat = new RPCAddressTable();
            RPCAddressOut<byte[], byte[]> m1 = new RPCAddressOut<byte[], byte[]>(rat);
            client.RPC.RegisterMethod(m1, 0, (db) =>
            {
                test = true;
                return db;
            });

            var task = client.RemoteGuaranteedExecution(m1, sendData);
            Task.Delay(Wait).Wait();

            //получить неоданую часть
            byte[] bb2 = Client._GetPacketSendBuffer(client).GetPacket(0, 1);
            accepter.Invoke(new AcceptData(bb2.Length, bb2));
            accepter.Invoke(new AcceptData(acp.sndDats[0].count, acp.sndDats[0].Data));
            Task.Delay(Wait * 3).Wait();

            //получить неоданую часть
            byte[] bb3 = Client._GetPacketSendBuffer(client).GetPacket(2, 1);
            accepter.Invoke(new AcceptData(bb3.Length, bb3));
            accepter.Invoke(new AcceptData(acp.sndDats[2].count, acp.sndDats[2].Data));
            Task.Delay(Wait).Wait();

            Debug.WriteLine("IMyAccepter->");
            foreach (var snd in acp.sndDats)
                Debug.WriteLine($"{snd.PacketType}  size:({snd.count})");

            if (task.IsCompleted)
            {
                for(int i = 1019; i < 1040; i++)
                {
                    Console.WriteLine($"[{i}]:\t{sendData[i]}\t{task.Result[i]}");
                }
                CollectionAssert.AreEqual(sendData, task.Result);
            }
            else
            {
                Assert.Fail("Задача не завершилась");
            }

            Assert.IsTrue(test);
            if (acp.sndDats.Count == 4)
            {
                Assert.IsTrue(acp.sndDats[0].PacketType == PacketType.SndGuaranteedRtrSegmented);
                Assert.IsTrue(acp.sndDats[1].PacketType == PacketType.SndDeliveryСompletedPackage);
                Assert.IsTrue(acp.sndDats[2].PacketType == PacketType.SndGuaranteedSegmentedReturned);
                Assert.IsTrue(acp.sndDats[3].PacketType == PacketType.SndDeliveryСompletedPackage);
            }
            else
                Assert.Fail("Ожидались другие пакеты!");
        }


        class Acp : IMyAccepter
        {
            public class SndDat
            {
                public byte[] Data;
                public int count;
                public PacketType PacketType;
            }

            public List<SndDat> sndDats = new List<SndDat>();
            public bool Stoped = false;

            public EndPoint EndPoint => new System.Net.IPEndPoint(System.Net.IPAddress.Any, 1);
            

            public int Receive(byte[] buffer)
            {
                throw new NotImplementedException();
            }

            public void Send(byte[] buffer, int count)
            {
                sndDats.Add(new SndDat()
                {
                    count = count,
                    Data = buffer,
                    PacketType = buffer.GetPacketType()
                });
            }

            public void Stop()
            {
                Stoped = true;
            }
        }
    }
}