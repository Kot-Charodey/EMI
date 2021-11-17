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
        /// Буфер для повторной отправки
        /// </summary>
        private PacketSendBuffer PacketSendBuffer;
        /// <summary>
        /// Буфер для сборки больших пакетов
        /// </summary>
        private readonly PacketAcceptBuffer PacketAcceptBuffer = new PacketAcceptBuffer();
        /// <summary>
        /// Буфер ожидания для возвращаемых функций
        /// </summary>
        private readonly ReturnWaiter ReturnWaiter = new ReturnWaiter();
        /// <summary>
        /// последний принятый ID + контроллер всего связанного с этим
        /// </summary>
        private AcceptID_Dispatcher AcceptID;
        /// <summary>
        /// Процесс запросса потерянных пакетов
        /// </summary>
        private Thread ThreadRequestLostPackages;
        /// <summary>
        /// Содержит список функций которые вызываються по packetType
        /// </summary>
        private Action<AcceptData>[] AcceptLogicEvent;

        /// <summary>
        /// Перед использованием клиента его необходимо инициализировать
        /// </summary>
        /// <param name="point"></param>
        private void InitAcceptLogicEvent(EndPoint point)
        {
            AcceptID = new AcceptID_Dispatcher(Accepter, SendID, null);
            PacketSendBuffer = new PacketSendBuffer(AcceptID);
            AcceptID.PacketSendBuffer = PacketSendBuffer;

            AcceptLogicEvent = new Action<AcceptData>[]
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
        private void ProcessAccept(AcceptData AcceptData)
        {
            try
            {
                AcceptLogicEvent[(int)AcceptData.PacketType].Invoke(AcceptData);
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
        //#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод

        /// <summary>
        /// Запрос - разрыв соединение + заполняет причину отключения
        /// </summary>
        private void SndClose(AcceptData AcceptData)
        {
            CloseReason = (CloseType)AcceptData.Buffer[1];
            Stop();
        }

        /// <summary>
        /// Запрос - просто выполнить
        /// </summary>
        private void SndSimple(AcceptData AcceptData)
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
        private void SndGuaranteed(AcceptData AcceptData)
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

            if (!AcceptID.CheckPacket(bitPacket.ID, bitPacket.PacketType))
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
        private void SndGuaranteedRtr(AcceptData AcceptData)
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

            if (!AcceptID.CheckPacket(bitPacket.ID, bitPacket.PacketType))
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

        private void SndGuaranteedSegmented(AcceptData AcceptData)
        {
            Packagers.Segmented.UnPack(AcceptData.Buffer, 0, out var bitPacket, out byte[] data);
            if (!AcceptID.PreCheckPacketSegment(bitPacket.ID))
                return;

            //если пакет готов
            if (PacketAcceptBuffer.BuildPackage(bitPacket, data, out var package))
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                AcceptID.DoneCheckPacketSegment(bitPacket.ID);

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
        }

        private void SndGuaranteedRtrSegmented(AcceptData AcceptData)
        {
            Packagers.Segmented.UnPack(AcceptData.Buffer, 0, out var bitPacket, out byte[] data);
            if (!AcceptID.PreCheckPacketSegment(bitPacket.ID))
                return;

            //если пакет готов
            if (PacketAcceptBuffer.BuildPackage(bitPacket, data, out var package))
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                AcceptID.DoneCheckPacketSegment(bitPacket.ID);

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
        }

        private void SndGuaranteedReturned(AcceptData AcceptData)
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

            if (!AcceptID.CheckPacket(bitPacket.ID, bitPacket.PacketType))
                return;

            ReturnWaiter.AddData(bitPacket.ReturnID, data);
        }

        private void SndGuaranteedSegmentedReturned(AcceptData AcceptData)
        {
            Packagers.SegmentedReturned.UnPack(AcceptData.Buffer, 0, out var bitPacket, out byte[] data);
            if (!AcceptID.PreCheckPacketSegment(bitPacket.ID))
                return;

            //если пакет готов
            if (PacketAcceptBuffer.BuildPackage(bitPacket, data, out var package))
            {
                //удаляем его из списка запрашиваемых (работает но это не точно (лень проверять (потом проверю)))
                AcceptID.DoneCheckPacketSegment(bitPacket.ID);

                ReturnWaiter.AddData(bitPacket.ReturnID, package.Data);
            }
        }

        //nope
        private void ReqConnection0(AcceptData AcceptData)
        {

        }

        //nope
        private void ReqConnection1(AcceptData AcceptData)
        {

        }

        //nope
        private void ReqConnection2(AcceptData AcceptData)
        {

        }

        /// <summary>
        /// Говорит о том что пакет полность доставлен
        /// </summary>
        private void SndDeliveryСompletedPackage(AcceptData AcceptData)
        {
            Packagers.SndDeliveryСompletedPackage.UnPack(AcceptData.Buffer, 0, out var bitPacket);

            if (!AcceptID.CheckPacket(bitPacket.ID, bitPacket.PacketType))
                return;

            PacketSendBuffer.RemovePacket(bitPacket.FullID);
        }

        /// <summary>
        /// Запрос - повторить отправку - потерянный пакет
        /// упаковка: /BitPacketGetPkg/,/ulong[] список айди пакетов/
        /// </summary>
        private void ReqGetPkg(AcceptData AcceptData)
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
        private void ReqGetPkgSegmented(AcceptData AcceptData)
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
        private void ReqPing0(AcceptData AcceptData)
        {
            AcceptData.Buffer[0] = (byte)PacketType.ReqPing1;
            Accepter.Send(AcceptData.Buffer, 1);
        }

        /// <summary>
        /// Измеряет пинг
        /// </summary>
        private void ReqPing1(AcceptData AcceptData)
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
//#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод

        /// <summary>
        /// запрашивает потерянные пакеты
        /// </summary>
        private void RequestLostPackages()
        {
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
                lock (AcceptID)
                {
                    //если всё на месте то проверяем не потерялся/появился новый пакет
                    if (AcceptID.LostID.Count == 0)
                    {
                        byte[] sendBuffer = Packagers.PacketGetPkg.PackUP(PacketType.ReqGetPkg, new ulong[1] { AcceptID.GetID() });
                        Accepter.Send(sendBuffer, sendBuffer.Length);
                    }
                    else
                    {
                        //запрос на первые несколько потерявшихся (не все сразу что бы сильно не забить канал)
                        int count = Math.Min(AcceptID.LostID.Count, 16);

                        //TODO БАГ если потерялось несколько сегментированных пакетов второй 3 и тд пакеты просит только сегмент №0 см условие в цикле

                        List<ulong> IDs = new List<ulong>(count);

                        foreach (var lost in AcceptID.LostID)
                        {
                            if (lost.Value) //если пакет сегментный
                            {
                                int[] segments = PacketAcceptBuffer.GetDownloadList(lost.Key, count);
                                if (segments.Length > 0)
                                {
                                    bitPacketReqGetPkgSegmented.ID = lost.Key;
                                    byte[] sendBufferS = Packagers.PkgSegmented.PackUP(bitPacketReqGetPkgSegmented, segments);
                                    Accepter.Send(sendBufferS, sendBufferS.Length);
                                }
                                count -= segments.Length;
                            }
                            else
                            {
                                IDs.Add(lost.Key);
                            }
                            count--;
                            if (count <= 0)
                                break;
                        }
                        //запрос на повтор пакетов
                        byte[] sendBuffer = Packagers.PacketGetPkg.PackUP(PacketType.ReqGetPkg, IDs.ToArray());
                        Accepter.Send(sendBuffer, sendBuffer.Length);
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
                //TODO убрать обработку пустого пакета (со стороны приёма тоже надо)
                //TODO прокоментировать ошибку
                throw new Exception("АААААААААААААААААААААААААААААААААААААААААААААААААААААААА");

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
    }
}