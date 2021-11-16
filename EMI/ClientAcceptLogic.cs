using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace EMI
{
    using Lower;
    using Lower.Package;
    using Lower.Buffer;

    /// <summary>
    /// Сетевой клиент EMI
    /// </summary>
    public partial class Client
    {

        /// <summary>
        /// последний принятый ID
        /// </summary>
        private readonly RefVarible<ulong> ReqID = new RefVarible<ulong>(0);
        /// <summary>
        /// Буфер для повторной отправки
        /// </summary>
        private readonly PacketSendBuffer PacketSendBuffer = new PacketSendBuffer();
        /// <summary>
        /// Буфер для сборки больших пакетов
        /// </summary>
        private readonly PacketAcceptBuffer PacketAcceptBuffer = new PacketAcceptBuffer();
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
        private Func<AcceptData, Task>[] AcceptLogicEvent;

        //TODO переименовать и расположить как в enum (реализации функцый желательно тоже)
        /// <summary>
        /// Перед использованием клиента его необходимо инициализировать
        /// </summary>
        /// <param name="point"></param>
        private void InitAcceptLogicEvent(EndPoint point)
        {
            AcceptLogicEvent = new Func<AcceptData, Task>[]
            {
                SndClose,
                SndSimple,
                SndGuaranteed,
                SndGuaranteedRtr,
                SndGuaranteedSegmented,
                SndGuaranteedRtrSegmented,
                SndGuaranteedReturned,
                SndGuaranteedSegmentedReturned,
                SndDeliveryСompletedPackage,
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

        /// <summary>
        /// если пришёл пакет
        /// </summary>
        /// <param name="AcceptData">Пакет для обработки</param>
        private async Task ProcessAccept(AcceptData AcceptData)
        {
            try
            {
                await AcceptLogicEvent[(int)AcceptData.PacketType].Invoke(AcceptData).ConfigureAwait(false);

                //AcceptLogicEvent[(int)packetType].Invoke();
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
                Console.WriteLine($"ProcessAccept -> ({AcceptData.PacketType}) ->" + e.ToString());
#endif      
                SendErrorClose(CloseType.StopPackageBad);
                Stop();
                throw e;
            }
        }
#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод

        /// <summary>
        /// Запрос - разрыв соединение + заполняет причину отключения
        /// </summary>
        private async Task SndClose(AcceptData AcceptData)
        {
            CloseReason = (CloseType)AcceptData.Buffer[1];
            Stop();
        }

        /// <summary>
        /// Запрос - просто выполнить
        /// </summary>
        private async Task SndSimple(AcceptData AcceptData)
        {
            BitPacketSimple bitPacket;
            byte[] data = null;

            if (AcceptData.Size > BitPacketSimple.SizeOf)
            {
                Packagers.Simple.UnPack(AcceptData.Buffer, 0, out bitPacket, out data);
            }
            else
            {
                Packagers.SimpleNoData.UnPack(AcceptData.Buffer, 0, out bitPacket);
            }

            ThreadPool.QueueUserWorkItem((object stateInfo) =>
            {
#if DEBUG
                try
                {
                    RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, false, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client -> SndSimple -> Execute -> Exception -> " + e.ToString());
                }
#else
                    RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, false, false);
#endif
            });

        }

        /// <summary>
        /// Запрос - выполнить с гарантией доставки
        /// </summary>
        /// <returns></returns>
        private async Task SndGuaranteed(AcceptData AcceptData)
        {
            BitPacketGuaranteed bitPacket;
            byte[] data = null;

            if (AcceptData.Size > BitPacketGuaranteed.SizeOf)
            {
                Packagers.Guaranteed.UnPack(AcceptData.Buffer, 0, out bitPacket, out data);
            }
            else
            {
                Packagers.GuaranteedNoData.UnPack(AcceptData.Buffer, 0, out bitPacket);
            }

            if (SubGuaranteedCheck(bitPacket.ID, false) == false)
                return;

            ThreadPool.QueueUserWorkItem((object stateInfo) =>
            {
#if DEBUG
                try
                {
                    RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, false, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client -> SndGuaranteed -> Execute -> Exception -> " + e.ToString());
                }
#else
                    RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, false, true);
#endif
            });

        }

        /// <summary>
        /// Запрос - выполнить с гарантией доставки + вернуть результат
        /// </summary>
        /// <returns></returns>
        private async Task SndGuaranteedRtr(AcceptData AcceptData)
        {
            BitPacketGuaranteed bitPacket;
            byte[] data = null;

            if (AcceptData.Size > BitPacketGuaranteed.SizeOf)
            {
                Packagers.Guaranteed.UnPack(AcceptData.Buffer, 0, out bitPacket, out data);
            }
            else
            {
                Packagers.GuaranteedNoData.UnPack(AcceptData.Buffer, 0, out bitPacket);
            }

            if (SubGuaranteedCheck(bitPacket.ID, false) == false)
                return;

            ThreadPool.QueueUserWorkItem(async (object stateInfo) =>
            {
#if DEBUG
                try
                {
                    await SendReturn(RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, true, true), bitPacket.ID).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client -> SndGuaranteedRtr -> Execute -> Exception -> " + e.ToString());
                }
#else
                    await SendReturn(RPC.Execute(LVL_Permission, data, bitPacket.RPCAddres, true, true), bitPacket.ID).ConfigureAwait(false);
#endif
            });

        }

        private async Task SndGuaranteedSegmented(AcceptData AcceptData)
        {
            Packagers.Segmented.UnPack(AcceptData.Buffer, 0, out var bitPacket, out byte[] data);
            //если пакет готов
            if (PacketAcceptBuffer.BuildPackage(bitPacket, data, out var package))
            {
                //TODO проверить
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                if (SubGuaranteedCheck(bitPacket.ID, true) == false)
                    return;

                ThreadPool.QueueUserWorkItem((object stateInfo) =>
                {
#if DEBUG
                    try
                    {
                        RPC.Execute(LVL_Permission, package.Data, package.RPCAddres, false, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client -> SndGuaranteedSegmented -> Execute -> Exception -> " + e.ToString());
                    }
#else
                    RPC.Execute(LVL_Permission, package.Data, package.RPCAddres, false, true);
#endif
                });

            }
            else
            {
                CheckFinderSegment(bitPacket.ID);
            }
        }

        private async Task SndGuaranteedRtrSegmented(AcceptData AcceptData)
        {
            Packagers.Segmented.UnPack(AcceptData.Buffer, 0, out var bitPacket, out byte[] data);
            //если пакет готов
            if (PacketAcceptBuffer.BuildPackage(bitPacket, data, out var package))
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                if (SubGuaranteedCheck(bitPacket.ID, true) == false)
                    return;

                ThreadPool.QueueUserWorkItem(async (object stateInfo) =>
                {
#if DEBUG
                    try
                    {
                        await SendReturn(RPC.Execute(LVL_Permission, package.Data, package.RPCAddres, true, true), package.ID).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client -> SndGuaranteedRtrSegmented -> Execute -> Exception -> " + e.ToString());
                    }
#else
                    await SendReturn(RPC.Execute(LVL_Permission, package.Data, package.RPCAddres, true, true), package.ID).ConfigureAwait(false);
#endif
                });

            }
            else
            {
                CheckFinderSegment(bitPacket.ID);
            }
        }

        private async Task SndGuaranteedReturned(AcceptData AcceptData)
        {
            BitPacketGuaranteedReturned bitPacket;
            byte[] data = null;

            if (AcceptData.Size > BitPacketGuaranteedReturned.SizeOf)
            {
                Packagers.GuaranteedReturned.UnPack(AcceptData.Buffer, 0, out bitPacket, out data);
            }
            else
            {
                Packagers.GuaranteedReturnedNoData.UnPack(AcceptData.Buffer, 0, out bitPacket);
            }

            if (SubGuaranteedCheck(bitPacket.ID, false) == false)
                return;

            ReturnWaiter.AddData(bitPacket.ReturnID, data);
        }

        private async Task SndGuaranteedSegmentedReturned(AcceptData AcceptData)
        {
            Packagers.SegmentedReturned.UnPack(AcceptData.Buffer, 0, out var bitPacket, out byte[] data);
            //если пакет готов
            if (PacketAcceptBuffer.BuildPackage(bitPacket, data, out var package))
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                if (SubGuaranteedCheck(bitPacket.ID, true) == false)
                    return;

                ReturnWaiter.AddData(bitPacket.ReturnID, package.Data);
            }
            else
            {
                CheckFinderSegment(bitPacket.ID);
            }
        }

        //nope
        private async Task ReqConnection0(AcceptData AcceptData)
        {

        }

        //nope
        private async Task ReqConnection1(AcceptData AcceptData)
        {

        }

        //nope
        private async Task ReqConnection2(AcceptData AcceptData)
        {

        }

        /// <summary>
        /// Говорит о том что пакет полность доставлен
        /// </summary>
        private async Task SndDeliveryСompletedPackage(AcceptData AcceptData)
        {
            Packagers.SndDeliveryСompletedPackage.UnPack(AcceptData.Buffer, 0, out var bitPacket);

            if (SubGuaranteedCheck(bitPacket.ID, false) == false)
                return;

            PacketSendBuffer.RemovePacket(bitPacket.FullID);
        }

        /// <summary>
        /// Запрос - повторить отправку - потерянный пакет
        /// упаковка: /BitPacketGetPkg/,/ulong[] список айди пакетов/
        /// </summary>
        private async Task ReqGetPkg(AcceptData AcceptData)
        {
            Packagers.PacketGetPkg.UnPack(AcceptData.Buffer, 0, out _, out ulong[] ID_Data);

            byte[] buffer;
            ulong id = SendID.GetID();
            for (int i = 0; i < ID_Data.Length; i++)
            {
                if (ID_Data[i] < id)
                {
                    buffer = PacketSendBuffer.GetPacket(ID_Data[i]);
                    if (buffer != null)
                    {
                        Accepter.Send(buffer, buffer.Length);
                    }
                }
                //если пакет не создан то промолчим
            }
        }

        /// <summary>
        ///  Запрос - повторить отправку - потерянный сегментированный пакет (так же если слишком много сегментов то изначально отправяться не все)
        ///  упаковка: /BitPacketGetPkgSegmented/,/ushort[] список сегментов/
        /// </summary>
        private async Task ReqGetPkgSegmented(AcceptData AcceptData)
        {
            Packagers.PkgSegmented.UnPack(AcceptData.Buffer, 0, out var bitPacket, out int[] ID_Data);

            for (int i = 0; i < ID_Data.Length; i++)
            {
                byte[] buffer = PacketSendBuffer.GetPacket(bitPacket.ID, ID_Data[i]);
                //если пакет не найден - выходим (мы уже его доставили (так нам сказал клиент))
                if (buffer == null)
                {
                    return;
                }
                //иначе отправим пакет повторно
                if (buffer != null)
                {
                    Accepter.Send(buffer, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Отражает запрос пинга обратно
        /// </summary>
        private async Task ReqPing0(AcceptData AcceptData)
        {
            AcceptData.Buffer[0] = (byte)PacketType.ReqPing1;
            Accepter.Send(AcceptData.Buffer, 1);
        }

        /// <summary>
        /// Измеряет пинг
        /// </summary>
        private async Task ReqPing1(AcceptData AcceptData)
        {
            var elips = StopwatchPing.Elapsed - EMI.Ping.RequestRateDelay[(int)RequestRatePing];
            //проверяем не устарел ли пакет
            if (elips.Ticks > 0)
            {
                StopwatchPing.Restart();
                Ping = (Ping + elips.TotalSeconds) * 0.5;
                PingMS = (PingMS + elips.TotalMilliseconds) * 0.5;
                PingIMS = (int)PingMS;
            }
        }
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод




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
                    if (!contains)
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
                lock (ReqID)
                {
                    lock (LostID)
                    {

                        //если всё на месте то проверяем не потерялся/появился новый пакет
                        if (LostID.Count == 0)
                        {
                            byte[] sendBuffer = Packagers.PacketGetPkg.PackUP(PacketType.ReqGetPkg, new ulong[1] { ReqID.Value });
                            Accepter.Send(sendBuffer, sendBuffer.Length);
                        }
                        else
                        {
                            //запрос на первые 64 потерявшихся (не все сразу что бы сильно не забить канал)
                            int count = 64;
                            if (LostID.Count < count)
                                count = LostID.Count;

                            //TODO БАГ если потерялось несколько сегментированных пакетов второй 3 и тд пакеты просит только сегмент №0 см условие в цикле

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

                            byte[] sendBuffer = Packagers.PacketGetPkg.PackUP(PacketType.ReqGetPkg, IDs.ToArray());
                            Accepter.Send(sendBuffer, sendBuffer.Length);

                            //если есть неполный сегментный пакет
                            if (IDBig != null)
                            {
                                lock (PacketAcceptBuffer)
                                {
                                    int[] segments = PacketAcceptBuffer.GetDownloadList(IDBig.Value, 64);
                                    if (segments.Length > 0)
                                    {
                                        bitPacketReqGetPkgSegmented.ID = IDBig.Value;
                                        sendBuffer = Packagers.PkgSegmented.PackUP(bitPacketReqGetPkgSegmented, segments);
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
        /// Упаковывает и отправляет ответные пакеты
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        private async Task SendReturn(byte[] data, ulong ID)
        {
            if (data != null)
            {
                //если пакет маленький то просто отправить
                if (data.Length <= 1024)
                {
                    BitPacketGuaranteedReturned bpgr = new BitPacketGuaranteedReturned()
                    {
                        PacketType = PacketType.SndGuaranteedReturned,
                        ID = SendID.GetNewID(),
                        ReturnID = ID,
                    };
                    byte[] PackData = Packagers.GuaranteedReturned.PackUP(bpgr, data);
                    await PacketSendBuffer.Storing(bpgr.ID, PackData).ConfigureAwait(false);
                    Accepter.Send(PackData, PackData.Length);
                }
                else//большой пакет
                {
                    //byte[] sndBuffer = new byte[1024];
                    //Array.Copy(data, sndBuffer, 1024);

                    BitPacketSegmentedReturned bp = new BitPacketSegmentedReturned()
                    {
                        PacketType = PacketType.SndGuaranteedSegmentedReturned,
                        ID = SendID.GetNewID(),
                        ReturnID = ID,
                        Segment = 0,
                        SegmentCount = BitPacketsUtilities.CalcSegmentCount(data.Length)
                    };

                    await PacketSendBuffer.Storing(bp.ID, bp, data).ConfigureAwait(false);

                    //data = Packagers.SegmentedReturned.PackUP(bp, sndBuffer);
                    //Accepter.Send(data, data.Length);
                    byte[] sndData = PacketSendBuffer.GetPacket(bp.ID, 0);
                    Accepter.Send(sndData, sndData.Length);
                }
            }
            else
            {
                BitPacketGuaranteedReturned bpgr = new BitPacketGuaranteedReturned()
                {
                    PacketType = PacketType.SndGuaranteedReturned,
                    ID = SendID.GetNewID(),
                    ReturnID = ID,
                };

                byte[] PackData = Packagers.GuaranteedReturnedNoData.PackUP(bpgr);
                await PacketSendBuffer.Storing(bpgr.ID, PackData).ConfigureAwait(false);
                Accepter.Send(PackData, PackData.Length);
            }
        }

        /// <summary>
        /// Проверяет потерян ли пакет и недоставлен ли он уже
        /// Так же указывает что данный пакет доставлен
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="SegmentPacket">Если пакет сегментированный он оставляет запрос в LostID даже когда считается не потерянным</param>
        /// <returns>если true то пакет пришёл в первые</returns>
        private bool SubGuaranteedCheck(ulong ID, bool SegmentPacket)
        {
            lock (ReqID)
            {
                if (ID == ReqID.Value)
                {
                    ReqID.Value++;

                    if (SegmentPacket)
                        lock (LostID)
                        {
                            for (int i = 0; i < LostID.Count; i++)
                            {
                                if (LostID[i].ID == ID)
                                {
                                    LostID.RemoveAt(i);
                                    break;
                                }
                            }
                        }
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
            }
            SendSndDeliveryСompletedPackage(ID);
            return true;
        }

        private void SendSndDeliveryСompletedPackage(ulong completID)
        {
            ulong sndID = SendID.GetNewID();
            var bp = new BitPackageDeliveryСompleted()
            {
                FullID = completID,
                ID = sndID,
                PacketType = PacketType.SndDeliveryСompletedPackage,
            };


            byte[] buffer = Packagers.SndDeliveryСompletedPackage.PackUP(bp);
            Task.Run(async () => await PacketSendBuffer.Storing(sndID, buffer));

            Accepter.Send(buffer, buffer.Length);
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
    }
}