using System;
using System.Threading.Tasks;

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
        private readonly SendID_Dispatcher SendID = new SendID_Dispatcher();

        #region Forwarding
        /// <summary>
        /// Для пересылки сообщений (адресс должен быть 1 и тот же и на сервере и на клиенте)
        /// </summary>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="sndData">Данные в запакованном ввиде</param>
        /// <param name="guaranteed">гарантийная доставка?</param>
        internal void RemoteForwardingExecution(ushort Address, byte[] sndData, bool guaranteed)
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
                        ForwardingGuaranteedNormal(Address, sndData);
                    }
                    else
                    {
                        ForwardingGuaranteedBig(Address, sndData);
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
                data = Packagers.SimpleNoData.PackUP(bps);
            }
            else//если вызов с данными
            {
                data = Packagers.Simple.PackUP(bps, sndData);
            }
            Accepter.Send(data, data.Length);
        }

        private void ForwardingGuaranteedNormal(ushort Address)
        {
            ulong id = SendID.GetNewID();
            BitPacketGuaranteed bpg = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packagers.GuaranteedNoData.PackUP(bpg);
            PacketSendBuffer.Storing(id, data).Wait();
            Accepter.Send(data, data.Length);
        }

        private void ForwardingGuaranteedNormal(ushort Address, byte[] sndData)
        {
            ulong id = SendID.GetNewID();
            BitPacketGuaranteed bpg = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address,
                ID = id
            };
            byte[] data = Packagers.Guaranteed.PackUP(bpg, sndData);
            PacketSendBuffer.Storing(id, data).Wait();
            Accepter.Send(data, data.Length);
        }

        private void ForwardingGuaranteedBig(ushort Address, byte[] sndData)
        {
            ulong id = SendID.GetNewID();
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


            PacketSendBuffer.Storing(id, bp, sndData).Wait();

            byte[] data = Packagers.Segmented.PackUP(bp, sndBuffer);
            Accepter.Send(data, data.Length);
        }

        #endregion

        #region Standard
        /// <summary>
        /// Выполнить RPC без гарантии доставки
        /// </summary>
        /// <param name="Address">Айди вызываемой функции</param>
        public void RemoteStandardExecution(RPCAddress Address)
        {
            BitPacketSimple bps = new BitPacketSimple()
            {
                PacketType = PacketType.SndSimple,
                RPCAddres = Address.ID,
            };
            byte[] data = Packagers.SimpleNoData.PackUP(bps);
            Accepter.Send(data, data.Length);
        }
        /// <summary>
        /// Выполнить RPC без гарантии доставки
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="t1">аргумент</param>
        public void RemoteStandardExecution<T1>(RPCAddress<T1> Address, T1 t1)
        {
            BitPacketSimple bps = new BitPacketSimple()
            {
                PacketType = PacketType.SndSimple,
                RPCAddres = Address.ID,
            };
            var pac = Packager.Create<T1>();

            byte[] data = Packagers.Simple.PackUP(bps, pac.PackUP(t1));
            Accepter.Send(data, data.Length);
        }
        #endregion
        #region Guaranteed 
        /// <summary>
        /// Выполнить RPC с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <param name="Address">Айди вызываемой функции</param>
        public async void RemoteGuaranteedExecution(RPCAddress Address)
        {
            ulong id = SendID.GetNewID();
            BitPacketGuaranteed bp = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteed,
                RPCAddres = Address.ID,
                ID = id
            };
            byte[] data = Packagers.GuaranteedNoData.PackUP(bp);
            await PacketSendBuffer.Storing(id, data);
            Accepter.Send(data, data.Length);
        }
        /// <summary>
        /// Выполнить RPC с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="t1">Aргумент</param>
        public async void RemoteGuaranteedExecution<T1>(RPCAddress<T1> Address, T1 t1)
        {
            ulong id = SendID.GetNewID();
            var pac = Packager.Create<T1>();
            long size = pac.CalcNeedSize(t1);
            byte[] data, buffer;
            if (size <= 1024)
            {
                BitPacketGuaranteed bp = new BitPacketGuaranteed()
                {
                    PacketType = PacketType.SndGuaranteed,
                    RPCAddres = Address.ID,
                    ID = id
                };
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);
                data = Packagers.Guaranteed.PackUP(bp, pac.PackUP(t1));
                await PacketSendBuffer.Storing(id, data);
                Accepter.Send(data, data.Length);
            }
            else
            {
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);

                byte[] sndBuffer = new byte[1024];
                Array.Copy(buffer, sndBuffer, 1024);

                BitPacketSegmented bp = new BitPacketSegmented()
                {
                    PacketType = PacketType.SndGuaranteedSegmented,
                    RPCAddres = Address.ID,
                    ID = id,
                    Segment = 0,
                    SegmentCount = BitPacketsUtilities.CalcSegmentCount(size)
                };

                await PacketSendBuffer.Storing(id, bp, buffer);

                data = Packagers.Segmented.PackUP(bp, sndBuffer);
                Accepter.Send(data, data.Length);
            }
        }
        #endregion
        #region Returned

        /// <summary>
        /// Выполнить RPC с возвратом результата с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="TOut">Тип возвращающегося результата</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <returns>Массив результата от выполнения всех функций (количество зависит от выполненных функций)</returns>
        public async Task<TOut> RemoteGuaranteedExecution<TOut>(RPCAddressOut<TOut> Address)
        {
            RPCfunctOut<Task<TOut>> waiter = null;
            ulong id = SendID.GetNewID();
            BitPacketGuaranteed bp = new BitPacketGuaranteed()
            {
                PacketType = PacketType.SndGuaranteedRtr,
                RPCAddres = Address.ID,
                ID = id
            };
            byte[] data = Packagers.GuaranteedNoData.PackUP(bp);
            await PacketSendBuffer.Storing(id, data);
            waiter = ReturnWaiter.SetupWaiting<TOut>(id);

            Accepter.Send(data, data.Length);

            return await waiter().ConfigureAwait(false);
        }

        /// <summary>
        /// Выполнить RPC с возвратом результата с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="TOut">Тип возвращающегося результата</typeparam>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="Address">Айди вызываемой функции</param>
        /// <param name="t1">Aргумент</param>
        /// <returns>Массив результата от выполнения всех функций (количество зависит от выполненных функций)</returns>
        public async Task<TOut> RemoteGuaranteedExecution<TOut, T1>(RPCAddressOut<TOut, T1> Address, T1 t1)
        {
            ulong id = SendID.GetNewID();
            var pac = Packager.Create<T1>();
            long size = pac.CalcNeedSize(t1);
            byte[] data, buffer;
            RPCfunctOut<Task<TOut>> waiter = null;

            if (size <= 1024)
            {
                BitPacketGuaranteed bp = new BitPacketGuaranteed()
                {
                    PacketType = PacketType.SndGuaranteedRtr,
                    RPCAddres = Address.ID,
                    ID = id
                };
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);
                data = Packagers.Guaranteed.PackUP(bp, pac.PackUP(t1));
                waiter = ReturnWaiter.SetupWaiting<TOut>(id);
                await PacketSendBuffer.Storing(id, data);

                Accepter.Send(data, data.Length);
            }
            else
            {
                buffer = new byte[size];
                pac.PackUP(buffer, 0, t1);

                byte[] sndBuffer = new byte[1024];
                Array.Copy(buffer, sndBuffer, 1024);

                BitPacketSegmented bp = new BitPacketSegmented()
                {
                    PacketType = PacketType.SndGuaranteedRtrSegmented,
                    RPCAddres = Address.ID,
                    ID = id,
                    Segment = 0,
                    SegmentCount = BitPacketsUtilities.CalcSegmentCount(size)
                };

                await PacketSendBuffer.Storing(id, bp, buffer);
                waiter = ReturnWaiter.SetupWaiting<TOut>(id);

                data = Packagers.Segmented.PackUP(bp, sndBuffer);
                Accepter.Send(data, data.Length);
            }

            return await waiter().ConfigureAwait(false);
        }
        #endregion
    }
}