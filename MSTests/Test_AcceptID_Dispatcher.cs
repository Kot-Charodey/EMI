using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Net;

namespace MSTests
{
    using EMI;
    using EMI.Lower;
    using EMI.Lower.Accepter;
    using EMI.Lower.Package;
    using EMI.Lower.Buffer;

    /// <summary>
    /// Проверка правильности заполнения полей пакетов (не должны пересекаться)
    /// </summary>
    [TestClass]
    public class Test_AcceptID_Dispatcher
    {
        [TestMethod]
        public void Test1_Send()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;


            Assert.IsTrue(dispatcher.GetID()==0,"в начале работы айди должен быть 0");
            dispatcher.CheckPacket(0,PacketType.SndGuaranteed);
            Assert.IsTrue(dispatcher.GetID() == 1, "прибыл пакет с айди 0! значит айди должен прибавиться");

            Assert.IsTrue(dispatcher.LostID.Count == 0,"ни один пакет не мог потеряться");
            Assert.IsTrue(acp.sndDats.Count == 1,"должно быть 1 отправленное сообщение");
            var packet = acp.sndDats[0];
            Assert.IsTrue(packet.PacketType==PacketType.SndDeliveryСompletedPackage,"сообщение о доставке пакета");
            Packagers.SndDeliveryСompletedPackage.UnPack(packet.Data, 0, out var bpdc);
            Assert.IsTrue(bpdc.FullID == 0,"пакета с айди 0");
        }

        [TestMethod]
        public void Test2_SendAndLose()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            Assert.IsTrue(dispatcher.GetID() == 0, "в начале работы айди должен быть 0");
            dispatcher.CheckPacket(1,PacketType.SndGuaranteed);
            Assert.IsTrue(dispatcher.GetID() == 2, "прибыл пакет с айди 1! значит айди должен прибавиться");

            Assert.IsTrue(dispatcher.LostID.Count == 1, "1 один пакет должен был потеряться");
            Assert.IsTrue(dispatcher.LostID.ContainsKey(0), "должен был потеряться пакет с айди 0");
            Assert.IsTrue(dispatcher.LostID[0]==false, "должен был потеряться пакет не сегментный");

            Assert.IsTrue(acp.sndDats.Count == 1, "должно быть 1 отправленное сообщение");
            var packet = acp.sndDats[0];
            Assert.IsTrue(packet.PacketType == PacketType.SndDeliveryСompletedPackage, "сообщение о доставке пакета");
            Packagers.SndDeliveryСompletedPackage.UnPack(packet.Data, 0, out var bpdc);
            Assert.IsTrue(bpdc.FullID == 1, "пакета с айди 1");
        }

        [TestMethod]
        public void Test3_SendAndLose2()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;


            Assert.IsTrue(dispatcher.GetID() == 0, "в начале работы айди должен быть 0");
            dispatcher.CheckPacket(2, PacketType.SndGuaranteed);
            Assert.IsTrue(dispatcher.GetID() == 3, "прибыл пакет с айди 1! значит айди должен прибавиться");

            Assert.IsTrue(dispatcher.LostID.Count == 2, "2 один пакет должен был потеряться");

            Assert.IsTrue(dispatcher.LostID.ContainsKey(0), "должен был потеряться пакет с айди 0");
            Assert.IsTrue(dispatcher.LostID[0] == false, "должен был потеряться пакет не сегментный");
            Assert.IsTrue(dispatcher.LostID.ContainsKey(1), "должен был потеряться пакет с айди 1");
            Assert.IsTrue(dispatcher.LostID[1] == false, "должен был потеряться пакет не сегментный");

            Assert.IsTrue(acp.sndDats.Count == 1, "должно быть 1 отправленное сообщение");
            var packet = acp.sndDats[0];
            Assert.IsTrue(packet.PacketType == PacketType.SndDeliveryСompletedPackage, "сообщение о доставке пакета");
            Packagers.SndDeliveryСompletedPackage.UnPack(packet.Data, 0, out var bpdc);
            Assert.IsTrue(bpdc.FullID == 2, "пакета с айди 2");
        }

        [TestMethod]
        public void Test4_SendRepetition()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            dispatcher.CheckPacket(0, PacketType.SndGuaranteed);
            dispatcher.CheckPacket(0, PacketType.SndGuaranteed);

            Assert.IsTrue(dispatcher.LostID.Count == 0, "ни один пакет не мог потеряться");
            Assert.IsTrue(acp.sndDats.Count == 1, "должно быть 1 отправленное сообщение");
            var packet = acp.sndDats[0];
            Assert.IsTrue(packet.PacketType == PacketType.SndDeliveryСompletedPackage, "сообщение о доставке пакета");
            Packagers.SndDeliveryСompletedPackage.UnPack(packet.Data, 0, out var bpdc);
            Assert.IsTrue(bpdc.FullID == 0, "пакета с айди 0");
        }

        [TestMethod]
        public void Test5_SendSegment()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            dispatcher.PreCheckPacketSegment(0);

            Assert.IsTrue(dispatcher.LostID.Count == 1);
            Assert.IsTrue(dispatcher.LostID.ContainsKey(0));
            Assert.IsTrue(dispatcher.LostID[0] == true);

            Assert.IsTrue(acp.sndDats.Count == 0);
        }

        [TestMethod]
        public void Test6_SendSegment()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            dispatcher.PreCheckPacketSegment(0);
            dispatcher.PreCheckPacketSegment(0);
            dispatcher.PreCheckPacketSegment(0);
            dispatcher.PreCheckPacketSegment(0);

            Assert.IsTrue(dispatcher.LostID.Count == 1);
            Assert.IsTrue(dispatcher.LostID.ContainsKey(0));
            Assert.IsTrue(dispatcher.LostID[0] == true);

            Assert.IsTrue(acp.sndDats.Count == 0);
        }

        [TestMethod]
        public void Test7_SendSegment()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            dispatcher.CheckPacket(1, PacketType.SndGuaranteed);
            dispatcher.PreCheckPacketSegment(0);

            Assert.IsTrue(dispatcher.LostID.Count == 1);
            Assert.IsTrue(dispatcher.LostID.ContainsKey(0));
            Assert.IsTrue(dispatcher.LostID[0] == true);

            Assert.IsTrue(acp.sndDats.Count == 1);
        }

        [TestMethod]
        public void Test8_SendSegment()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            dispatcher.PreCheckPacketSegment(0);
            dispatcher.PreCheckPacketSegment(0);

            Assert.IsTrue(dispatcher.LostID.Count == 1);
            Assert.IsTrue(dispatcher.LostID.ContainsKey(0));
            Assert.IsTrue(dispatcher.LostID[0] == true);
            Assert.IsTrue(acp.sndDats.Count == 0);

            dispatcher.DoneCheckPacketSegment(0);
            Assert.IsTrue(dispatcher.LostID.Count == 0);
            Assert.IsTrue(acp.sndDats.Count == 1);
        }

        [TestMethod]
        public void Test9_SendSegment()
        {
            Acp acp = new Acp();
            SendID_Dispatcher sendID = new SendID_Dispatcher();

            AcceptID_Dispatcher dispatcher = new AcceptID_Dispatcher(acp, sendID, null);
            PacketSendBuffer buffer = new PacketSendBuffer(dispatcher);
            dispatcher.PacketSendBuffer = buffer;

            dispatcher.CheckPacket(1, PacketType.SndGuaranteed);
            Assert.IsTrue(dispatcher.PreCheckPacketSegment(0) == true);
            Assert.IsTrue(dispatcher.PreCheckPacketSegment(0) == true);
            dispatcher.CheckPacket(2, PacketType.SndGuaranteed);
            Assert.IsTrue(dispatcher.PreCheckPacketSegment(0) == true);
            dispatcher.DoneCheckPacketSegment(0);
            Assert.IsTrue(dispatcher.PreCheckPacketSegment(0) == false);
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
