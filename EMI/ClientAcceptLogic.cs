using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using SpeedByteConvector;
using System.Net;

namespace EMI
{
    using Lower;
    using Lower.Package;


    /// <summary>
    /// Сетевой клиент EMI
    /// </summary>
    public partial class Client
    {
        /// <summary>
        /// последний принятый ID
        /// </summary>
        private ulong ReqID;
        /// <summary>
        /// Буфер для повторной отправки
        /// </summary>
        private SendBackupBuffer SendBackupBuffer;
        /// <summary>
        /// Буфер для сборки больших пакетов
        /// </summary>
        private SegmentPackagesBuffer SegmentPackagesBuffer;
        /// <summary>
        /// Буфер ожидания для возвращаемых функций
        /// </summary>
        private ReturnWaiter ReturnWaiter;
        /// <summary>
        /// Список потерянных пакетов
        /// </summary>
        private readonly List<ulong> LostID = new List<ulong>();
        private Thread ThreadRequestLostPackages;

        private Action[] AcceptLogicEvent;

        private void InitAcceptLogicEvent(EndPoint point)
        {
            AcceptLogicEvent = new Action[]
            {
                SndStandard,
                SndGuaranteed,
                SndGuaranteedSegmented,
                ReqConnection,
                ReqConnectionGood,
                SndClose,
                ReqGetPkgGuaranted,
                SndGuaranteedReturned,
                SndGuaranteedSegmentedReturned,
                ReqPing0,
                ReqPing1
            };

            ReturnWaiter = new ReturnWaiter();
            SendBackupBuffer = new SendBackupBuffer();
            SegmentPackagesBuffer = new SegmentPackagesBuffer();
            ThreadRequestLostPackages = new Thread(RequestLostPackages)
            {
                Name = "EMI.Client.ThreadRequestLostPackages [" + point.ToString() + "]",
                IsBackground = true
            };
        }

        private PacketType packetType;
        private byte[] AcceptBuffer;

        /// <summary>
        /// если пришёл пакет
        /// </summary>
        /// <param name="buffer"></param>
        private void ProcessAccept(byte[] buffer)
        {
            try
            {
                AcceptBuffer = buffer;
                packetType = BitPacketsUtilities.GetPacketType(buffer);
                //выполняем обработку в соотведствии с типом
                AcceptLogicEvent[(int)packetType].Invoke();
            }
            catch (Exception e)
            {
                SendErrorClose(CloseType.StopPackageBad);
                IsConnect = false;
                throw e;
            }
        }

        private void SndStandard()
        {
            var bitPacket = PackConvector.UnPackJust<BitPacketSimple>(AcceptBuffer);
            byte[] UnPackBuffer = bitPacket.GetByteData();

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                unsafe
                {
                    fixed (byte* buff = &UnPackBuffer[0])
                    {
                        RPC.Execute(LVL_Permission, buff);
                    }
                }
            });
        }

        /// <summary>
        /// Проверяет потерян ли пакет
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        private bool SubGuaranteedCheck(ulong ID)
        {
            if (ID == ReqID)
            {
                ReqID++;
            }
            else if (ID > ReqID)
            {
                //если пакет черезур новый надо запросить более старые
                LostPackagesStartFind(ReqID, ID - 1);
                ReqID = ID + 1;
            }
            else
            {
                //пакет устарел или пришёл потерянный
                lock (LostID)
                {
                    if (LostID.Contains(ID))
                    {
                        LostID.Remove(ID);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        private void SndGuaranteed()
        {
            var bitPacket = PackConvector.UnPackJust<BitPacketMedium>(AcceptBuffer);
            byte[] UnPackBuffer = bitPacket.GetByteData();

            if (SubGuaranteedCheck(bitPacket.ID) == false)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                PackageToReturnData packageToReturnData = null;
                unsafe
                {
                    fixed (byte* buff = &UnPackBuffer[0])
                    {
                        packageToReturnData = RPC.Execute(LVL_Permission, buff);
                    }
                }
                SendReturn(packageToReturnData, bitPacket.ID);
            });
        }

        private void SndGuaranteedSegmented()
        {
            var bitPacket = PackConvector.UnPackJust<BitPacketBig>(AcceptBuffer);
            byte[] UnPackBuffer = bitPacket.GetByteData();

            if (SubGuaranteedCheck(bitPacket.ID) == false)
            {
                return;
            }

            byte[] buffer = SegmentPackagesBuffer.AddSegment(in bitPacket);

            if (buffer != null)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    PackageToReturnData packageToReturnData = null;
                    unsafe
                    {
                        fixed (byte* buff = &buffer[0])
                        {
                            packageToReturnData = RPC.Execute(LVL_Permission, buff);
                        }
                    }
                    SendReturn(packageToReturnData, bitPacket.MainID);
                });
            }
        }

        private void ReqConnection()
        {

        }

        private void ReqConnectionGood()
        {

        }

        /// <summary>
        /// Запрос - разрыв соединение
        /// </summary>
        private void SndClose()
        {
            var bitPacket = PackConvector.UnPackJust<BitPacketSimple>(AcceptBuffer);
            unsafe
            {
                CloseReason = (CloseType)bitPacket.ByteData[0];
            }
            Stop();
        }

        /// <summary>
        /// Запрос - повторить отправку - потерянный пакет
        /// </summary>
        private unsafe void ReqGetPkgGuaranted()
        {
            var bitPacket = PackConvector.UnPackJust<BitPacketSimple>(AcceptBuffer);
            ReqGetPkgListID rgplID = *((ReqGetPkgListID*)bitPacket.ByteData);

            byte[] buffer;
            for (int i = 0; i < rgplID.CountID; i++)
            {
                buffer = SendBackupBuffer.Get(rgplID.IDData[i]);
                //если пакета нет и он был создан
                if (buffer == null && rgplID.IDData[i] < SendID)
                {
                    SendErrorClose(CloseType.StopPackageDestroyed);
                    Stop();
                }
                //иначе отправим пакет повторно
                if (buffer != null)
                {
                    Accepter.Send(buffer, buffer.Length);
                }
            }
        }

        private void SndGuaranteedReturned()
        {
            var bitPacket = PackConvector.UnPackJust<BitPacketMedium>(AcceptBuffer);
            byte[] UnPackBuffer = bitPacket.GetByteData();

            if (SubGuaranteedCheck(bitPacket.ID) == false)
            {
                return;
            }

            unsafe
            {
                PackageReturned* package = (PackageReturned*)bitPacket.ByteData;
                ReturnWaiter.AddData(package->ID, bitPacket.GetByteData());
            }
        }

        private void SndGuaranteedSegmentedReturned()
        {

        }

        /// <summary>
        /// Отражает запрос пинга обратно
        /// </summary>
        private void ReqPing0()
        {
            AcceptBuffer[0] = (byte)PacketType.ReqPing1;
            Accepter.Send(AcceptBuffer, 3);
        }

        /// <summary>
        /// Измеряет пинг
        /// </summary>
        private void ReqPing1()
        {
            var elips = StopwatchPing.Elapsed;
            StopwatchPing.Restart();
            Ping = (Ping + elips.TotalSeconds) * 0.5;
            PingMS = (PingMS + elips.TotalMilliseconds) * 0.5;
            PingIMS = (int)PingMS;
        }

        /// <summary>
        /// Добавляет в список потерянные пакеты и будит поток по их поиску
        /// </summary>
        /// <param name="startID"></param>
        /// <param name="endID"></param>
        private void LostPackagesStartFind(ulong startID, ulong endID)
        {
            lock (LostID)
            {
                for (ulong i = startID; i <= endID; i++)
                {
                    if (!LostID.Contains(i))
                    {
                        LostID.Add(i);
                    }
                }
            }
        }
        /// <summary>
        /// запрашивает потерянные пакеты
        /// </summary>
        private unsafe void RequestLostPackages()
        {
            while (IsConnect)
            {
                //если давно нет соединения
                if (StopwatchPing.Elapsed.TotalMilliseconds > TimeOutPing)
                {
                    SendErrorClose(CloseType.StopConnectionError);
                    Stop();
                }

                lock (LostID)
                {
                    //если всё на месте то проверяем не потерялся ли новый пакет
                    if (LostID.Count == 0)
                    {
                        var bitPacketSimple = new BitPacketSimple(PacketType.ReqGetPkgGuaranted, 9);
                        ReqGetPkgListID* rgplID = (ReqGetPkgListID*)bitPacketSimple.ByteData;
                        ulong* point = (ulong*)((IntPtr)rgplID + 1);
                        *point = ReqID;
                        
                        byte[] sendBuffer = bitPacketSimple.GetAllBytes();
                        Accepter.Send(sendBuffer, sendBuffer.Length);
                    }
                    else
                    {
                        //запрос на первые 100 потерявшихся
                        int count = 100;
                        if (LostID.Count < count)
                            count = LostID.Count;
                        
                        var bitPacketSimple = new BitPacketSimple(PacketType.ReqGetPkgGuaranted);
                        ReqGetPkgListID* rgplID = (ReqGetPkgListID*)bitPacketSimple.ByteData;


                        ulong* point = (ulong*)((IntPtr)rgplID + 1);

                        rgplID->CountID=(byte)count;

                        for (int i = 0; i < count; i++)
                        {
                            *point = LostID[i];
                            point++;
                        }
                        
                        bitPacketSimple.ByteDataLength = rgplID->GetSize();
                        
                        byte[] sendBuffer = bitPacketSimple.GetAllBytes();
                        Accepter.Send(sendBuffer, sendBuffer.Length);
                    }
                }

                Thread.Sleep(50);
            }
        }

        private unsafe void SendReturn(PackageToReturnData rtrd,ulong ID)
        {
            if (rtrd.NeedReturn)
            {
                if (rtrd.AllSize <= 1024)
                {
                    byte[] dataM = new byte[rtrd.AllSize + BitPacketMedium.FieldOffsetData];
                    fixed(byte* data = &dataM[0])
                    {
                        BitPacketMedium* bitPacket = (BitPacketMedium*)data;
                        bitPacket->ID = GetID();
                        bitPacket->PacketType = PacketType.SndGuaranteedReturned;
                        bitPacket->ByteDataLength = rtrd.AllSize;

                        PackageReturned* package = (PackageReturned*)(data+BitPacketMedium.FieldOffsetDataStart);
                        package->ArgumentCount = (byte)rtrd.Data.Length;
                        package->ID = ID;

                        int sm = BitPacketMedium.FieldOffsetDataStart + sizeof(PackageReturned);
                        BitArgument* argument = (BitArgument*)(data+sm);
                        
                        int sizeArg = sizeof(BitArgument);
                        
                        for (int i = 0; i < package->ArgumentCount; i++)
                        {
                            argument->Size = (ushort)(rtrd.Data[i].Length + sizeArg);
                            fixed (byte* sour = &rtrd.Data[i][0]) {
                                Buffer.MemoryCopy(sour, data + sm + sizeArg, argument->Size - 1, argument->Size - 1);
                            }
                            sm += argument->Size;
                            argument = (BitArgument*)(data + sm);
                        }

                        SendBackupBuffer.Add(bitPacket->ID, dataM);
                        Accepter.Send(dataM, dataM.Length);
                    }
                }
                else
                {
                    throw new NotImplementedException("СЛОООЖНО, потом сделаю");
                }
            }
        }
    }
}