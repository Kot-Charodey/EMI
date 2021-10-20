using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using SmartPackager;

namespace EMI
{
    using Lower;
    using Lower.Package;

    /// <summary>
    /// Сетевой клиент EMI
    /// </summary>
    public partial class Client
    {
        //SmartPackager - создаёт упаковщики для дальнейшего использования
        private static readonly Packager.M<BitPacketReqGetPkgSegmented, ushort[]> Packager_PkgSegmented = Packager.Create<BitPacketReqGetPkgSegmented, ushort[]>();
        private static readonly Packager.M<PacketType, ulong[]> Packager_PacketGetPkg = Packager.Create<PacketType, ulong[]>();
        private static readonly Packager.M<BitPacketSegmented, byte[]> Packager_Segmented = Packager.Create<BitPacketSegmented, byte[]>();
        private static readonly Packager.M<BitPacketSegmentedReturned, byte[]> Packager_SegmentedReturned = Packager.Create<BitPacketSegmentedReturned, byte[]>();
        private static readonly Packager.M<BitPacketSimple> Packager_SimpleNoData = Packager.Create<BitPacketSimple>();
        private static readonly Packager.M<BitPacketSimple, byte[]> Packager_Simple = Packager.Create<BitPacketSimple, byte[]>();
        private static readonly Packager.M<BitPacketGuaranteed> Packager_GuaranteedNoData = Packager.Create<BitPacketGuaranteed>();
        private static readonly Packager.M<BitPacketGuaranteed, byte[]> Packager_Guaranteed = Packager.Create<BitPacketGuaranteed, byte[]>();
        private static readonly Packager.M<BitPacketGuaranteedReturned> Packager_GuaranteedReturnedNoData = Packager.Create<BitPacketGuaranteedReturned>();
        private static readonly Packager.M<BitPacketGuaranteedReturned, byte[]> Packager_GuaranteedReturned = Packager.Create<BitPacketGuaranteedReturned, byte[]>();
        private static readonly Packager.M<BitPacketSndFullyReceivedSegmentPackage> Packager_SndFullyReceivedSegmentPackage = Packager.Create<BitPacketSndFullyReceivedSegmentPackage>();

        /// <summary>
        /// последний принятый ID
        /// </summary>
        private readonly RefVarible<ulong> ReqID = new RefVarible<ulong>(0);
        /// <summary>
        /// Буфер для повторной отправки
        /// </summary>
        private readonly SendBackupBuffer SendBackupBuffer = new SendBackupBuffer();
        /// <summary>
        /// Буфер для отправкпи
        /// </summary>
        private readonly SendSegmentBackupBuffer SendSegmentBackupBuffer = new SendSegmentBackupBuffer();
        /// <summary>
        /// Буфер для сборки больших пакетов
        /// </summary>
        private readonly SegmentPackagesBuffer SegmentPackagesBuffer = new SegmentPackagesBuffer();
        /// <summary>
        /// Буфер ожидания для возвращаемых функций
        /// </summary>
        private readonly ReturnWaiter ReturnWaiter = new ReturnWaiter();
        /// <summary>
        /// Список потерянных пакетов
        /// </summary>
        private readonly List<LostPackageInfo> LostID = new List<LostPackageInfo>();
        /// <summary>
        /// Процесс запросса потерянных пакетов
        /// </summary>
        private Thread ThreadRequestLostPackages;
        /// <summary>
        /// Содержит список функций которые вызываються по packetType
        /// </summary>
        private Action[] AcceptLogicEvent;

        //TODO переименовать и расположить как в enum (реализации функцый желательно тоже)
        /// <summary>
        /// Перед использованием клиента его необходимо инициализировать
        /// </summary>
        /// <param name="point"></param>
        private void InitAcceptLogicEvent(EndPoint point)
        {
            AcceptLogicEvent = new Action[]
            {
                SndClose,
                SndSimple,
                SndGuaranteed,
                SndGuaranteedRtr,
                SndGuaranteedSegmented,
                SndGuaranteedRtrSegmented,
                SndGuaranteedReturned,
                SndGuaranteedSegmentedReturned,
                SndFullyReceivedSegmentPackage,
                ReqGetPkg,
                ReqGetPkgSegmented,
                ReqPing0,
                ReqPing1,
                ReqConnection0,
                ReqConnection1,
                ReqConnection2,
            };

            ThreadRequestLostPackages = new Thread(RequestLostPackages)
            {
                Name = "EMI.Client.ThreadRequestLostPackages [" + point.ToString() + "]",
                IsBackground = true
            };
        }

        private PacketType packetType;
        private byte[] AcceptBuffer;
        private int SizeAcceptBuffer;

        /// <summary>
        /// если пришёл пакет
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size">размер пакета</param>
        private void ProcessAccept(byte[] buffer, int size)
        {
            try
            {
                AcceptBuffer = buffer;
                SizeAcceptBuffer = size;
                packetType = buffer.GetPacketType();

                if(packetType!=PacketType.ReqGetPkg && packetType!=PacketType.ReqPing0 && packetType != PacketType.ReqPing1)
                Console.WriteLine(packetType);
                //выполняем обработку в соотведствии с типом
                AcceptLogicEvent[(int)packetType].Invoke();
            }
            catch (OperationCanceledException)
            {
                SendErrorClose(CloseType.NormalStop);
                Stop();
                throw new OperationCanceledException();
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine($"ProcessAccept -> ({packetType}) ->" + e.ToString());
#endif
                SendErrorClose(CloseType.StopPackageBad);
                Stop();
                throw e;
            }
        }

        /// <summary>
        /// Проверяет потерян ли пакет и недоставлен ли он уже
        /// Так же указывает что данный пакет доставлен
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        private bool SubGuaranteedCheck(ulong ID)
        {
            lock (ReqID)
            {
                if (ID == ReqID.Value)
                {
                    ReqID.Value++;
                }
                else if (ID > ReqID.Value)
                {
                    //если пакет черезур новый надо запросить более старые
                    LostPackagesStartFind(ReqID.Value, ID - 1);
                    ReqID.Value = ID + 1;
                }
                else
                {
                    //пакет устарел (уже приходил) или пришёл потерянный
                    lock (LostID)
                    {
                        bool remove = false;
                        int i;
                        for (i = 0; i < LostID.Count; i++)
                        {
                            if (LostID[i].ID == ID)
                            {
                                remove = true;
                                break;
                            }
                        }

                        if (remove)
                        {
                            LostID.RemoveAt(i);
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Указывает что данный пакет сегментный для запроса всех его частей
        /// </summary>
        private void CheckFinderSegment(ulong ID)
        {
            lock (LostID)
            {
                for (int j = 0; j < LostID.Count; j++)
                {
                    if (LostID[j].ID == ID)
                    {
                        LostID[j].IsSegment = true;
                        return;
                    }
                }
                //если пакета нет в списке потерянных добавляем что бы загрузить все его сегменты
                LostID.Add(new LostPackageInfo(ID, true));
            }
        }

        /// <summary>
        /// Запрос - разрыв соединение
        /// </summary>
        private void SndClose()
        {
            CloseReason = (CloseType)AcceptBuffer[1];
            Stop();
        }

        /// <summary>
        /// Запрос - просто выполнить
        /// </summary>
        private unsafe void SndSimple()
        {
            BitPacketSimple bitPacket;
            byte[] data = null;

            if (SizeAcceptBuffer > sizeof(BitPacketSimple))
            {
                Packager_Simple.UnPack(AcceptBuffer, 0, out bitPacket, out data);
            }
            else
            {
                Packager_SimpleNoData.UnPack(AcceptBuffer, 0, out bitPacket);
            }

            ThreadPool.QueueUserWorkItem((object stateInfo) =>
            {
                RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, false, false);
            });

        }

        private unsafe void SndGuaranteed()
        {
            BitPacketGuaranteed bitPacket;
            byte[] data = null;

            if (SizeAcceptBuffer > sizeof(BitPacketGuaranteed))
            {
                Packager_Guaranteed.UnPack(AcceptBuffer, 0, out bitPacket, out data);
            }
            else
            {
                Packager_GuaranteedNoData.UnPack(AcceptBuffer, 0, out bitPacket);
            }

            if (SubGuaranteedCheck(bitPacket.ID) == false)
                return;

            ThreadPool.QueueUserWorkItem((object stateInfo) =>
            {
                try
                {
                    RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, false, true);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Client -> SndGuaranteed -> Execute -> Exception -> " + e.ToString());
                }
            });

        }

        private unsafe void SndGuaranteedRtr()
        {
            BitPacketGuaranteed bitPacket;
            byte[] data = null;

            if (SizeAcceptBuffer > sizeof(BitPacketGuaranteed))
            {
                Packager_Guaranteed.UnPack(AcceptBuffer, 0, out bitPacket, out data);
            }
            else
            {
                Packager_GuaranteedNoData.UnPack(AcceptBuffer, 0, out bitPacket);
            }

            if (SubGuaranteedCheck(bitPacket.ID) == false)
                return;

            ThreadPool.QueueUserWorkItem((object stateInfo) =>
            {
                SendReturn(RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, true, true), bitPacket.ID);
            });

        }

        private void SndGuaranteedSegmented()
        {
            Packager_Segmented.UnPack(AcceptBuffer, 0, out var bitPacket, out byte[] data);
            Console.WriteLine("segment -> " + bitPacket.Segment + " " + bitPacket.SegmentCount + " " + bitPacket.ID);
            var package = SegmentPackagesBuffer.AddSegment(in bitPacket, data);
            //если пакет готов
            if (package != null)
            {
                //TODO проверить
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                if (SubGuaranteedCheck(bitPacket.ID) == false)
                    return;

                ThreadPool.QueueUserWorkItem((object stateInfo) =>
                {
                    try
                    {
                        RPC.Execute(LVL_Permission, package.Data, package.RPCAddres, false, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client -> SndGuaranteedSegmented -> Execute -> Exception -> " + e.ToString());
                    }
                });

            }
            else
            {
                CheckFinderSegment(bitPacket.ID);
            }
        }


        private void SndGuaranteedRtrSegmented()
        {
            Packager_Segmented.UnPack(AcceptBuffer, 0, out var bitPacket, out byte[] data);
            var package = SegmentPackagesBuffer.AddSegment(in bitPacket, data);

            //если пакет готов
            if (package != null)
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                if (SubGuaranteedCheck(bitPacket.ID) == false)
                    return;

                ThreadPool.QueueUserWorkItem((object stateInfo) =>
                {
                    SendReturn(RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, true, true), package.ID);
                });

            }
            else
            {
                CheckFinderSegment(bitPacket.ID);
            }
        }

        private unsafe void SndGuaranteedReturned()
        {
            BitPacketGuaranteedReturned bitPacket;
            byte[] data = null;

            if (SizeAcceptBuffer > sizeof(BitPacketGuaranteedReturned))
            {
                Packager_GuaranteedReturned.UnPack(AcceptBuffer, 0, out bitPacket, out data);
            }
            else
            {
                Packager_GuaranteedReturnedNoData.UnPack(AcceptBuffer, 0, out bitPacket);
            }

            if (SubGuaranteedCheck(bitPacket.ID) == false)
                return;

            ReturnWaiter.AddData(bitPacket.ReturnID, data);
        }

        private void SndGuaranteedSegmentedReturned()
        {
            Packager_SegmentedReturned.UnPack(AcceptBuffer, 0, out var bitPacket, out byte[] data);
            var package = SegmentPackagesBuffer.AddSegment(in bitPacket, data);

            //если пакет готов
            if (package != null)
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                if (SubGuaranteedCheck(bitPacket.ID) == false)
                    return;

                ReturnWaiter.AddData(bitPacket.ReturnID, data);
            }
            else
            {
                CheckFinderSegment(bitPacket.ID);
            }
        }

        //nope
        private void ReqConnection0()
        {

        }

        //nope
        private void ReqConnection1()
        {

        }

        //nope
        private void ReqConnection2()
        {

        }

        /// <summary>
        ///  Запрос - повторить отправку - потерянный сегментированный пакет (так же если слишком много сегментов то изначально отправяться не все)
        ///  упаковка: /BitPacketGetPkgSegmented/,/ushort[] список сегментов/
        /// </summary>
        private void ReqGetPkgSegmented()
        {
            Packager_PkgSegmented.UnPack(AcceptBuffer, 0, out var bitPacket, out ushort[] ID_Data);

            for (int i = 0; i < ID_Data.Length; i++)
            {
                var info = SendSegmentBackupBuffer.Get(bitPacket.ID, ID_Data[i], out byte[] buffer);
                //если пакет не найден - он потерян, ошибка
                if (buffer == null)
                {
                    SendErrorClose(CloseType.StopPackageDestroyed);
                    Stop();
                }
                //иначе отправим пакет повторно
                if (buffer != null)
                {
                    if (info.IsReturned) //если возвращаймый сегиентированный
                    {
                        var head = info.BPSR;
                        head.Segment = ID_Data[i];

                        byte[] sndBuffer = Packager_SegmentedReturned.PackUP(head, buffer);
                        Accepter.Send(sndBuffer, sndBuffer.Length);


                    }
                    else//если обычный
                    {
                        var head = info.BPS;
                        head.Segment = ID_Data[i];

                        byte[] sndBuffer = Packager_Segmented.PackUP(head, buffer);
                        Accepter.Send(sndBuffer, sndBuffer.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Запрос - повторить отправку - потерянный пакет
        /// упаковка: /BitPacketGetPkg/,/ulong[] список айди пакетов/
        /// </summary>
        private void ReqGetPkg()
        {
            Packager_PacketGetPkg.UnPack(AcceptBuffer, 0, out _, out ulong[] ID_Data);

            byte[] buffer;
            ulong id = SendID.GetID();
            for (int i = 0; i < ID_Data.Length; i++)
            {

                buffer = SendBackupBuffer.Get(ID_Data[i]);
                //если пакета нет и он был создан - попробуем поискать среди больших пакетов
                if (buffer == null && ID_Data[i] < id)
                {
                    //предположим что клиент не знает о типе пакета "сегментный" поэтому отправим первый сегмент что бы он знал что запрашивать далее (через ReqGetPkgSegmented)
                    var info = SendSegmentBackupBuffer.Get(ID_Data[i], 0, out buffer);
                    //если пакет не найден значит он уничтожен и придёться разорвать соединение
                    if (buffer == null)
                    {
                        SendErrorClose(CloseType.StopPackageDestroyed);
                        Stop();
                    }
                    //отправка сегиентированного пакета
                    if (info.IsReturned) //если возвращаймый сегиентированный
                    {
                        byte[] sndBuffer = Packager_SegmentedReturned.PackUP(info.BPSR, buffer);
                        Accepter.Send(sndBuffer, sndBuffer.Length);


                    }
                    else//если обычный сегментный
                    {
                        byte[] sndBuffer = Packager_Segmented.PackUP(info.BPS, buffer);
                        Accepter.Send(sndBuffer, sndBuffer.Length);
                    }
                }
                else if (buffer != null)//иначе отправим пакет повторно
                {
                    Accepter.Send(buffer, buffer.Length);
                }
                //если пакет не создан то промолчим
            }
        }

        /// <summary>
        /// Отражает запрос пинга обратно
        /// </summary>
        private void ReqPing0()
        {
            AcceptBuffer[0] = (byte)PacketType.ReqPing1;
            Accepter.Send(AcceptBuffer, 1);
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
        /// Добавляет в список потерянные пакеты и будит поток по их поиску (не указывает что пакет сегментный - это должен делать SndGuaranteedSegmented и SndGuaranteedSegmentedReturned)
        /// </summary>
        /// <param name="startID"></param>
        /// <param name="endID"></param>
        private void LostPackagesStartFind(ulong startID, ulong endID)
        {
            lock (LostID)
            {
                for (ulong i = startID; i <= endID; i++)
                {
                    bool contains = false;
                    for (int j = 0; j < LostID.Count; j++)
                    {
                        if (LostID[j].ID == i)
                        {
                            contains = true;
                            break;
                        }
                    }
                    if (contains)
                        LostID.Add(new LostPackageInfo(i, false));
                }
            }
        }

        /// <summary>
        /// запрашивает потерянные пакеты
        /// </summary>
        private void RequestLostPackages()
        {
            LostPackageInfo lpi;
            BitPacketReqGetPkgSegmented bitPacketReqGetPkgSegmented = new BitPacketReqGetPkgSegmented()
            {
                PacketType = PacketType.ReqGetPkgSegmented
            };
            while (IsConnect)
            {
                //если давно нет соединения
#if !NoPingLimit
                if (StopwatchPing.Elapsed.TotalMilliseconds > TimeOutPing)
                {
                    SendErrorClose(CloseType.StopConnectionError);
                    Stop();
                }
#endif
                lock (LostID)
                {
                    lock (ReqID)
                    {
                        //если всё на месте то проверяем не потерялся/появился новый пакет
                        if (LostID.Count == 0)
                        {

                            byte[] sendBuffer = Packager_PacketGetPkg.PackUP(PacketType.ReqGetPkg, new ulong[1] { ReqID.Value });
                            Accepter.Send(sendBuffer, sendBuffer.Length);
                        }
                        else
                        {
                            //запрос на первые 64 потерявшихся (не все сразу что бы сильно не забить канал)
                            int count = 64;
                            if (LostID.Count < count)
                                count = LostID.Count;

                            List<ulong> IDs = new List<ulong>(count);
                            //для сегментных пакетов
                            ulong? IDBig = null;

                            for (int i = 0; i < count; i++)
                            {
                                lpi = LostID[i];
                                if (lpi.IsSegment && IDBig == null)
                                {
                                    IDBig = lpi.ID;
                                }
                                else
                                {
                                    IDs.Add(lpi.ID);
                                }
                            }

                            byte[] sendBuffer = Packager_PacketGetPkg.PackUP(PacketType.ReqGetPkg, IDs.ToArray());
                            Accepter.Send(sendBuffer, sendBuffer.Length);

                            //если есть неполный сегментный пакет
                            if (IDBig != null)
                            {
                                lock (SegmentPackagesBuffer)
                                {
                                    ushort[] segments = SegmentPackagesBuffer.GetDownloadList(64, IDBig.Value);
                                    if (segments != null)
                                    {
                                        bitPacketReqGetPkgSegmented.ID = IDBig.Value;
                                        sendBuffer = Packager_PkgSegmented.PackUP(bitPacketReqGetPkgSegmented, segments);
                                        Accepter.Send(sendBuffer, sendBuffer.Length);
                                    }
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Говорит о том что сегментный пакет полность доставлен
        /// </summary>
        private void SndFullyReceivedSegmentPackage()
        {
            Packager_SndFullyReceivedSegmentPackage.UnPack(AcceptBuffer, 0, out var bitPacket);

            if (SubGuaranteedCheck(bitPacket.ID) == false)
                return;

            SendSegmentBackupBuffer.Remove(bitPacket.FullID);
        }

        private void SendReturn(byte[] data, ulong ID)
        {
            if (data != null)
            {
                //если пакет маленький то просто отправить
                if (data.Length <= 1024)
                {
                    BitPacketGuaranteedReturned bpgr = new BitPacketGuaranteedReturned()
                    {
                        PacketType = PacketType.SndGuaranteedReturned,
                        ID = SendID.GetNewIDAndLock(),
                        ReturnID = ID,
                        ReturnNull = false
                    };
                    byte[] PackData = Packager_GuaranteedReturned.PackUP(bpgr, data);
                    SendBackupBuffer.Add(bpgr.ID, PackData);
                    SendID.UnlockID();
                    Accepter.Send(PackData, PackData.Length);
                }
                else if (data.Length <= 67107840) //если пакет жирный (64 MB)
                {
                    byte[] sndBuffer = new byte[1024];
                    Array.Copy(data, sndBuffer, 1024);

                    BitPacketSegmentedReturned bp = new BitPacketSegmentedReturned()
                    {
                        PacketType = PacketType.SndGuaranteedSegmentedReturned,
                        ID = SendID.GetNewIDAndLock(),
                        ReturnID = ID,
                        Segment = 0,
                        SegmentCount = BitPacketsUtilities.CalcSegmentCount(data.Length)
                    };

                    SendSegmentBackupBuffer.Add(ID, data, bp);
                    SendID.UnlockID();

                    data = Packager_SegmentedReturned.PackUP(bp, sndBuffer);
                    Accepter.Send(data, data.Length);
                }
                else //если пакет ОЧЕНЬ ЖИРНЫЙ
                {
                    throw new InsufficientMemoryException("SendReturn() -> Size > 67107840 bytes (64 MB)");
                }
            }
            else
            {
                BitPacketGuaranteedReturned bpgr = new BitPacketGuaranteedReturned()
                {
                    PacketType = PacketType.SndGuaranteedReturned,
                    ID = SendID.GetNewIDAndLock(),
                    ReturnID = ID,
                    ReturnNull = true
                };

                byte[] PackData = Packager_GuaranteedReturnedNoData.PackUP(bpgr);
                SendBackupBuffer.Add(bpgr.ID, PackData);
                SendID.UnlockID();
                Accepter.Send(PackData, PackData.Length);
            }
        }
    }
}