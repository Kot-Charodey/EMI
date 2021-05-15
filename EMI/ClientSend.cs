using System;

namespace EMI
{
    using Lower.Package;
    using SmartPackager;

    public partial class Client
    {
        /// <summary>
        /// АЙДИ для отправки сообщений (нужен для отслеживания "ПРИШЛИ ЛИ ВСЕ ПАКЕТЫ")
        /// </summary>
        private ulong SendID;

        private ulong GetID()
        {
            lock (SendBackupBuffer)
            {
                return SendID++;
            }
        }

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
            ulong id = GetID();
            BitPacketGuaranteed bp = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packager_GuaranteedNoData.PackUP(bp);
            SendBackupBuffer.Add(id, data);
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
            ulong id = GetID();
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
                Accepter.Send(data, data.Length);
            }
            else if (size <= 67107840) //(64 MB)
            {
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);

                SendSegmentBackupBuffer.Add(id, buffer);
                byte[] sndBuffer = new byte[1024];
                Array.Copy(buffer, sndBuffer, 1024);

                BitPacketSegmented bp = new BitPacketSegmented()
                {
                    PacketType = PacketType.SndGuaranteedSegmented,
                    RPCAddres = Address,
                    ID = id,
                    Segment = 0,
                    SegmentCount = (ushort)(size / ushort.MaxValue)
                };
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
            ulong id = GetID();
            BitPacketGuaranteed bp = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteedRtr,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packager_GuaranteedNoData.PackUP(bp);
            SendBackupBuffer.Add(id, data);
            Accepter.Send(data, data.Length);

            return ReturnWaiter.Wait<TOut>(id);
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
            ulong id = GetID();
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
                Accepter.Send(data, data.Length);
            }
            else if (size <= 67107840) //(64 MB)
            {
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);

                SendSegmentBackupBuffer.Add(id, buffer);
                byte[] sndBuffer = new byte[1024];
                Array.Copy(buffer, sndBuffer, 1024);

                BitPacketSegmented bp = new BitPacketSegmented()
                {
                    PacketType = PacketType.SndGuaranteedRtrSegmented,
                    RPCAddres = Address,
                    ID = id,
                    Segment = 0,
                    SegmentCount = (ushort)(size / ushort.MaxValue)
                };
                data = Packager_Segmented.PackUP(bp, sndBuffer);
                Accepter.Send(data, data.Length);
            }
            else
            {
                throw new InsufficientMemoryException("Execution -> Size > 67107840 bytes (64 MB)");
            }

            return ReturnWaiter.Wait<TOut>(id);
        }
        #endregion
    }
}