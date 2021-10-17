using System;

namespace EMI
{
    using Lower;
    using Lower.Package;
    using SmartPackager;

    public partial class Client
    {
        /// <summary>
        /// АЙДИ для отправки сообщений (нужен для отслеживания "ПРИШЛИ ЛИ ВСЕ ПАКЕТЫ")
        /// </summary>
        private readonly SendID_Dispatcher SendID=new SendID_Dispatcher();

        #region Forwarding
        /// <summary>
        /// Для пересылки сообщений (адресс должен быть 1 и тот же и на сервере и на клиенте)
        /// </summary>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="sndData">Данные в запакованном ввиде</param>
        /// <param name="guaranteed">гарантийная доставка?</param>
        internal void RemoteForwardingExecution(ushort Address,byte[] sndData,bool guaranteed)
        {
            //если гарантия доставки
            if (guaranteed)
            {
                if (sndData == null)//если просто вызов без данных
                {
                    ForwardingGuaranteedNormal(Address);
                }
                else//если вызов с данными
                {
                    if (sndData.Length <= 1024)
                    {
                        ForwardingGuaranteedNormal(Address,sndData);
                    }
                    else if (sndData.Length <= 67107840)
                    {
                        ForwardingGuaranteedBig(Address, sndData);
                    }
                    else
                    {
                        throw new InsufficientMemoryException("Execution -> Size > 67107840 bytes (64 MB)");
                    }
                }
                
            }
            else//если просто отправить
            {
                ForwardingStandard(Address, sndData);
            }
        }

        private void ForwardingStandard(ushort Address, byte[] sndData)
        {
            BitPacketSimple bps = new BitPacketSimple()
            {
                PacketType = PacketType.SndSimple,
                RPCAddres = Address,
            };
            byte[] data;


            if (sndData == null)//если просто вызов без данных
            {
                data = Packager_SimpleNoData.PackUP(bps);
            }
            else//если вызов с данными
            {
                data = Packager_Simple.PackUP(bps, sndData);
            }
            Accepter.Send(data, data.Length);
        }

        private void ForwardingGuaranteedNormal(ushort Address)
        {
            ulong id = SendID.GetNewIDAndLock();
            BitPacketGuaranteed bpg = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packager_GuaranteedNoData.PackUP(bpg);
            SendBackupBuffer.Add(id, data);
            SendID.UnlockID();
            Accepter.Send(data, data.Length);
        }

        private void ForwardingGuaranteedNormal(ushort Address, byte[] sndData)
        {
            ulong id = SendID.GetNewIDAndLock();
            BitPacketGuaranteed bpg = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packager_Guaranteed.PackUP(bpg,sndData);
            SendBackupBuffer.Add(id, data);
            SendID.UnlockID();
            Accepter.Send(data, data.Length);
        }

        private void ForwardingGuaranteedBig(ushort Address, byte[] sndData)
        {
            ulong id = SendID.GetNewIDAndLock();
            byte[] sndBuffer = new byte[1024];
            Array.Copy(sndData, sndBuffer, 1024);

            BitPacketSegmented bp = new BitPacketSegmented()
            {
                PacketType = PacketType.SndGuaranteedSegmented,
                RPCAddres = Address,
                ID = id,
                Segment = 0,
                SegmentCount = BitPacketsUtilities.CalcSegmentCount(sndData.Length)
            };


            SendSegmentBackupBuffer.Add(id, sndData, bp);
            SendID.UnlockID();

            byte[] data = Packager_Segmented.PackUP(bp, sndBuffer);
            Accepter.Send(data, data.Length);
        }

        #endregion

        #region Standard
        /// <summary>
        /// Выполнить RPC без гарантии доставки
        /// </summary>
        /// <param name="Address">Айди вызываемой функции</param>
        public void RemoteStandardExecution(ushort Address)
        {
            BitPacketSimple bps = new BitPacketSimple()
            {
                PacketType = PacketType.SndSimple,
                RPCAddres = Address,
            };
            byte[] data = Packager_SimpleNoData.PackUP(bps);
            Accepter.Send(data, data.Length);
        }
        /// <summary>
        /// Выполнить RPC без гарантии доставки
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="t1">аргумент</param>
        public void RemoteStandardExecution<T1>(ushort Address, T1 t1)
        {
            BitPacketSimple bps = new BitPacketSimple()
            {
                PacketType = PacketType.SndSimple,
                RPCAddres = Address,
            };
            var pac = Packager.Create<T1>();

            byte[] data = Packager_Simple.PackUP(bps, pac.PackUP(t1));
            Accepter.Send(data, data.Length);
        }
        #endregion
        #region Guaranteed 
        /// <summary>
        /// Выполнить RPC с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <param name="Address">Айди вызываемой функции</param>
        public void RemoteGuaranteedExecution(ushort Address)
        {
            ulong id = SendID.GetNewIDAndLock();
            BitPacketGuaranteed bp = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packager_GuaranteedNoData.PackUP(bp);
            SendBackupBuffer.Add(id, data);
            SendID.UnlockID();
            Accepter.Send(data, data.Length);
        }
        /// <summary>
        /// Выполнить RPC с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="t1">Aргумент</param>
        public void RemoteGuaranteedExecution<T1>(ushort Address, T1 t1)
        {
            ulong id = SendID.GetNewIDAndLock();
            var pac = Packager.Create<T1>();
            long size = pac.CalcNeedSize(t1);
            byte[] data, buffer;
            if (size <= 1024)
            {
                BitPacketGuaranteed bp = new BitPacketGuaranteed()
                {
                    PacketType = PacketType.SndGuaranteed,
                    RPCAddres = Address,
                    ID = id
                };
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);
                data = Packager_Guaranteed.PackUP(bp, pac.PackUP(t1));
                SendBackupBuffer.Add(id, data);
                SendID.UnlockID();
                Accepter.Send(data, data.Length);
            }
            else if (size <= 67107840) //(64 MB)
            {
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);

                byte[] sndBuffer = new byte[1024];
                Array.Copy(buffer, sndBuffer, 1024);

                BitPacketSegmented bp = new BitPacketSegmented()
                {
                    PacketType = PacketType.SndGuaranteedSegmented,
                    RPCAddres = Address,
                    ID = id,
                    Segment = 0,
                    SegmentCount = BitPacketsUtilities.CalcSegmentCount(size)
                };

                SendSegmentBackupBuffer.Add(id, buffer, bp);
                SendID.UnlockID();

                data = Packager_Segmented.PackUP(bp, sndBuffer);
                Accepter.Send(data, data.Length);
            }
            else
            {
                throw new InsufficientMemoryException("Execution -> Size > 67107840 bytes (64 MB)");
            }
        }
        #endregion
        #region Returned
        /// <summary>
        /// Выполнить RPC с возвратом результата (поток блокируется на время ожидания результата)с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="TOut">Тип возвращающегося результата</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <returns>Массив результата от выполнения всех функций (количество зависит от выполненных функций)</returns>
        public TOut RemoteGuaranteedExecution<TOut>(ushort Address)
        {
            ulong id = SendID.GetNewIDAndLock();
            BitPacketGuaranteed bp = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteedRtr,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packager_GuaranteedNoData.PackUP(bp);
            SendBackupBuffer.Add(id, data);
            SendID.UnlockID();
            return ReturnWaiter.Wait<TOut>(id, () => Accepter.Send(data, data.Length));
        }

        /// <summary>
        /// Выполнить RPC с возвратом результата (поток блокируется на время ожидания результата)с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="TOut">Тип возвращающегося результата</typeparam>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="t1">Aргумент</param>
        /// <returns>Массив результата от выполнения всех функций (количество зависит от выполненных функций)</returns>
        public TOut RemoteGuaranteedExecution<TOut, T1>(ushort Address, T1 t1)
        {
            ulong id = SendID.GetNewIDAndLock();
            var pac = Packager.Create<T1>();
            long size = pac.CalcNeedSize(t1);
            byte[] data, buffer;
            if (size <= 1024)
            {
                BitPacketGuaranteed bp = new BitPacketGuaranteed()
                {
                    PacketType = PacketType.SndGuaranteedRtr,
                    RPCAddres = Address,
                    ID = id
                };
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);
                data = Packager_Guaranteed.PackUP(bp, pac.PackUP(t1));
                SendBackupBuffer.Add(id, data);
                SendID.UnlockID();
            }
            else if (size <= 67107840) //(64 MB)
            {
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);

                byte[] sndBuffer = new byte[1024];
                Array.Copy(buffer, sndBuffer, 1024);

                BitPacketSegmented bp = new BitPacketSegmented()
                {
                    PacketType = PacketType.SndGuaranteedRtrSegmented,
                    RPCAddres = Address,
                    ID = id,
                    Segment = 0,
                    SegmentCount = BitPacketsUtilities.CalcSegmentCount(size)
                };

                SendSegmentBackupBuffer.Add(id, buffer, bp);
                SendID.UnlockID();

                data = Packager_Segmented.PackUP(bp, sndBuffer);
            }
            else
            {
                throw new InsufficientMemoryException("Execution -> Size > 67107840 bytes (64 MB)");
            }

            return ReturnWaiter.Wait<TOut>(id,()=> Accepter.Send(data, data.Length));
        }
        #endregion
    }
}