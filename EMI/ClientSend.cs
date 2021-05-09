using System;

namespace EMI
{
    using Lower;
    using Lower.Package;

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
        /// <param name="ID">Айди вызываемой функции</param>
        public unsafe void RemoteStandardExecution(ushort ID)
        {
            var bitPacket = new BitPacketSimple(PacketType.SndSimple, sizeof(Package));
            Package* package = (Package*)bitPacket.ByteData;

            package->RPC_ID = ID;
            package->CameBack = PackageCameBack.No;
            package->ArgumentCount = 0;

            byte[] bufferSend = bitPacket.GetAllBytes();
            Accepter.Send(bufferSend, bufferSend.Length);
        }
        /// <summary>
        /// Выполнить RPC без гарантии доставки
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="ID">Айди вызываемой функции</param>
        /// <param name="t1">аргумент</param>
        public unsafe void RemoteStandardExecution<T1>(ushort ID, T1 t1) where T1 : unmanaged
        {
            var bitPacket = new BitPacketSimple(PacketType.SndSimple);
            Package* package = (Package*)bitPacket.ByteData;

            int argSize = sizeof(BitArgument);
            int AllSize = sizeof(Package) + argSize + sizeof(T1);

            if (AllSize > 1024)
            {
                throw new InsufficientMemoryException("Size>1024");
            }

            BitArgument* point = (BitArgument*)((IntPtr)package + sizeof(Package));

            package->RPC_ID = ID;
            package->CameBack = PackageCameBack.No;
            package->ArgumentCount = 1;

            //упаковка аргументов
            point->Size = (ushort)(sizeof(T1) + argSize);
            T1* arg = (T1*)((IntPtr)point + argSize);
            *arg = t1;
            point = (BitArgument*)((long)point + point->Size);
            //======

            bitPacket.ByteDataLength = (ushort)AllSize;



            byte[] bufferSend = bitPacket.GetAllBytes();
            Accepter.Send(bufferSend, bufferSend.Length);
        }

        /// <summary>
        /// Выполнить RPC без гарантии доставки
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="ID">Айди вызываемой функции</param>
        /// <param name="t1">аргумент (Массив)</param>
        public unsafe void RemoteStandardExecution<T1>(ushort ID, T1[] t1) where T1 : unmanaged
        {
            var bitPacket = new BitPacketSimple(PacketType.SndSimple);
            Package* package = (Package*)bitPacket.ByteData;

            int argSize = sizeof(BitArgument);
            int AllSize = sizeof(Package) + argSize + sizeof(int) + argSize +  sizeof(T1);

            if (AllSize > 1024)
            {
                throw new InsufficientMemoryException("Size>1024");
            }

            BitArgument* point = (BitArgument*)((IntPtr)package + sizeof(Package));

            package->RPC_ID = ID;
            package->CameBack = PackageCameBack.No;
            package->ArgumentCount = 1;

            //упаковка аргументов
            point->Size = (ushort)(sizeof(int) + argSize);
            int* arg1 = (int*)((IntPtr)point + argSize);
            *arg1 = t1.Length;
            point = (BitArgument*)((long)point + point->Size);
            
            point->Size = (ushort)(sizeof(T1) + argSize);
            T1* arg2 = (T1*)((IntPtr)point + argSize);
            int arrT1Size = t1.Length * sizeof(T1);
            fixed (T1* arr1 = &t1[0])
                Buffer.MemoryCopy(arr1, arg2, arrT1Size, arrT1Size);
            //======

            bitPacket.ByteDataLength = (ushort)AllSize;



            byte[] bufferSend = bitPacket.GetAllBytes();
            Accepter.Send(bufferSend, bufferSend.Length);
        }
        #endregion
        #region Guaranteed 
        /// <summary>
        /// Выполнить RPC с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <param name="ID">Айди вызываемой функции</param>
        public unsafe void RemoteGuaranteedExecution(ushort ID)
        {
            ulong id = GetID();

            var bitPacket = new BitPacketMedium(PacketType.SndGuaranteed, id, sizeof(Package));
            Package* package = (Package*)bitPacket.ByteData;

            package->RPC_ID = ID;
            package->CameBack = PackageCameBack.No;
            package->ArgumentCount = 0;

            byte[] bufferSend = bitPacket.GetAllBytes();
            SendBackupBuffer.Add(id, bufferSend);
            Accepter.Send(bufferSend, bufferSend.Length);
        }
        /// <summary>
        /// Выполнить RPC с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="ID">Айди вызываемой функции</param>
        /// <param name="t1">Aргумент</param>
        public unsafe void RemoteGuaranteedExecution<T1>(ushort ID, T1 t1) where T1 : unmanaged
        {
            ulong LowID = GetID();

            int argSize = sizeof(BitArgument);
            int AllSize = sizeof(Package) + argSize + sizeof(T1);

            if (AllSize <= 1024)
            {
                var BitPacketMedium = new BitPacketMedium(PacketType.SndGuaranteed, LowID, sizeof(Package));
                Package* package = (Package*)BitPacketMedium.ByteData;

                BitArgument* point = (BitArgument*)((IntPtr)package + sizeof(Package));

                package->RPC_ID = ID;
                package->ArgumentCount = 1;
                package->CameBack = PackageCameBack.No;

                //упаковка аргументов
                point->Size = (ushort)(sizeof(T1) + argSize);
                AllSize += point->Size;
                T1* arg = (T1*)((IntPtr)point + argSize);
                *arg = t1;
                point = (BitArgument*)((long)point + point->Size);
                //===================

                BitPacketMedium.ByteDataLength = (ushort)AllSize;
                byte[] bufferSend = BitPacketMedium.GetAllBytes();
                SendBackupBuffer.Add(LowID, bufferSend);
                Accepter.Send(bufferSend, bufferSend.Length);
            }
            else if (AllSize <= ushort.MaxValue)
            {
                byte[] buffer = new byte[AllSize];
                fixed (byte* bp = &buffer[0])
                {
                    Package* package = (Package*)bp;
                    BitArgument* point = (BitArgument*)((IntPtr)package + sizeof(Package));

                    package->RPC_ID = ID;
                    package->ArgumentCount = 1;
                    package->CameBack = PackageCameBack.No;

                    //упаковка аргументов
                    point->Size = (ushort)(sizeof(T1) + argSize);
                    AllSize += point->Size;
                    T1* arg = (T1*)((IntPtr)point + argSize);
                    *arg = t1;
                    point = (BitArgument*)((long)point + point->Size);
                    //===================


                    BitPacketSegmented bitPacket;
                    int count;
                    int segmentCount = buffer.Length / 1024;
                    if (segmentCount < buffer.Length / 1024f)
                    {
                        segmentCount++;
                    }

                    byte[] buffer2 = null;
                    ulong Main = LowID;
                    ushort pch = 0;
                    for (int i = 0; i < buffer.Length; i += 1024)
                    {
                        count = buffer.Length - i;
                        if (count > 1024) count = 1024;
                        bitPacket = new BitPacketBig(PacketType.SndGuaranteedSegmented, LowID, Main, bp, i, count, pch++, (ushort)segmentCount);
                        if (buffer2 == null || buffer2.Length != bitPacket.GetSizeOf())
                        {
                            buffer2 = new byte[bitPacket.GetSizeOf()];
                        }
                        bitPacket.GetAllBytes(buffer2);
                        SendBackupBuffer.Add(LowID, buffer2);
                        Accepter.Send(buffer2, buffer2.Length);

                        LowID = GetID();
                    }
                }
            }
            else
            {
                throw new InsufficientMemoryException("Size>65535");
            }
        }
        #endregion
        #region Returned
        /// <summary>
        /// Выполнить RPC с возвратом результата (поток блокируется на время ожидания результата)с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="TOut">Тип возвращающегося результата</typeparam>
        /// <param name="ID">Айди вызываемой функции</param>
        /// <returns>Массив результата от выполнения всех функций (количество зависит от выполненных функций)</returns>
        public unsafe TOut[] RemoteGuaranteedExecution<TOut>(ushort ID)
            where TOut : unmanaged
        {
            ulong LowID = GetID();

            int argSize = sizeof(BitArgument);
            int AllSize = sizeof(Package) + argSize;

            if (AllSize <= 1024)
            {
                var BitPacketMedium = new BitPacketMedium(PacketType.SndGuaranteed, LowID, sizeof(Package));
                Package* package = (Package*)BitPacketMedium.ByteData;

                package->RPC_ID = ID;
                package->ArgumentCount = 0;
                package->CameBack = PackageCameBack.NeedCameBack;

                BitPacketMedium.ByteDataLength = (ushort)AllSize;
                byte[] bufferSend = BitPacketMedium.GetAllBytes();
                SendBackupBuffer.Add(LowID, bufferSend);
                Accepter.Send(bufferSend, bufferSend.Length);

                return ReturnWaiter.Wait<TOut>(LowID);
            }
            else if (AllSize <= ushort.MaxValue)
            {
                ulong Main = LowID;
                byte[] buffer = new byte[AllSize];
                fixed (byte* bp = &buffer[0])
                {
                    Package* package = (Package*)bp;

                    package->RPC_ID = ID;
                    package->ArgumentCount = 0;
                    package->CameBack = PackageCameBack.NeedCameBack;


                    BitPacketSegmented bitPacket;
                    int count;
                    int segmentCount = buffer.Length / 1024;
                    if (segmentCount < buffer.Length / 1024f)
                    {
                        segmentCount++;
                    }

                    byte[] buffer2 = null;
                    ushort pch = 0;
                    for (int i = 0; i < buffer.Length; i += 1024)
                    {
                        count = buffer.Length - i;
                        if (count > 1024) count = 1024;
                        bitPacket = new BitPacketBig(PacketType.SndGuaranteedSegmented, LowID, Main, bp, i, count, pch++, (ushort)segmentCount);
                        if (buffer2 == null || buffer2.Length != bitPacket.GetSizeOf())
                        {
                            buffer2 = new byte[bitPacket.GetSizeOf()];
                        }
                        bitPacket.GetAllBytes(buffer2);
                        SendBackupBuffer.Add(LowID, buffer2);
                        Accepter.Send(buffer2, buffer2.Length);

                        LowID = GetID();
                    }
                }

                return ReturnWaiter.Wait<TOut>(Main);
            }
            else
            {
                throw new InsufficientMemoryException("Size>65535");
            }
        }

        /// <summary>
        /// Выполнить RPC с возвратом результата (поток блокируется на время ожидания результата)с гарантией доставки (последовательность вызовов не гарантируется)
        /// </summary>
        /// <typeparam name="TOut">Тип возвращающегося результата</typeparam>
        /// <typeparam name="T1">тип аргумента</typeparam>
        /// <param name="ID">Айди вызываемой функции</param>
        /// <param name="t1">Aргумент</param>
        /// <returns>Массив результата от выполнения всех функций (количество зависит от выполненных функций)</returns>
        public unsafe TOut[] RemoteGuaranteedExecution<TOut,T1>(ushort ID, T1 t1) 
            where TOut : unmanaged
            where T1 : unmanaged
        {
            ulong LowID = GetID();

            int argSize = sizeof(BitArgument);
            int AllSize = sizeof(Package) + argSize + sizeof(T1);

            if (AllSize <= 1024)
            {
                var BitPacketMedium = new BitPacketMedium(PacketType.SndGuaranteed, LowID, sizeof(Package));
                Package* package = (Package*)BitPacketMedium.ByteData;

                BitArgument* point = (BitArgument*)((IntPtr)package + sizeof(Package));

                package->RPC_ID = ID;
                package->ArgumentCount = 1;
                package->CameBack = PackageCameBack.NeedCameBack;

                //упаковка аргументов
                point->Size = (ushort)(sizeof(T1) + argSize);
                AllSize += point->Size;
                T1* arg = (T1*)((IntPtr)point + argSize);
                *arg = t1;
                point = (BitArgument*)((long)point + point->Size);
                //===================

                BitPacketMedium.ByteDataLength = (ushort)AllSize;
                byte[] bufferSend = BitPacketMedium.GetAllBytes();
                SendBackupBuffer.Add(LowID, bufferSend);
                Accepter.Send(bufferSend, bufferSend.Length);

                return ReturnWaiter.Wait<TOut>(LowID);
            }
            else if (AllSize <= ushort.MaxValue)
            {
                ulong Main = LowID;
                byte[] buffer = new byte[AllSize];
                fixed (byte* bp = &buffer[0])
                {
                    Package* package = (Package*)bp;
                    BitArgument* point = (BitArgument*)((IntPtr)package + sizeof(Package));

                    package->RPC_ID = ID;
                    package->ArgumentCount = 1;
                    package->CameBack = PackageCameBack.NeedCameBack;

                    //упаковка аргументов
                    point->Size = (ushort)(sizeof(T1) + argSize);
                    AllSize += point->Size;
                    T1* arg = (T1*)((IntPtr)point + argSize);
                    *arg = t1;
                    point = (BitArgument*)((long)point + point->Size);
                    //===================


                    BitPacketSegmented bitPacket;
                    int count;
                    int segmentCount = buffer.Length / 1024;
                    if (segmentCount < buffer.Length / 1024f)
                    {
                        segmentCount++;
                    }

                    byte[] buffer2 = null;
                    ushort pch = 0;
                    for (int i = 0; i < buffer.Length; i += 1024)
                    {
                        count = buffer.Length - i;
                        if (count > 1024) count = 1024;
                        bitPacket = new BitPacketBig(PacketType.SndGuaranteedSegmented, LowID, Main, bp, i, count, pch++, (ushort)segmentCount);
                        if (buffer2 == null || buffer2.Length != bitPacket.GetSizeOf())
                        {
                            buffer2 = new byte[bitPacket.GetSizeOf()];
                        }
                        bitPacket.GetAllBytes(buffer2);
                        SendBackupBuffer.Add(LowID, buffer2);
                        Accepter.Send(buffer2, buffer2.Length);

                        LowID = GetID();
                    }
                }

                return ReturnWaiter.Wait<TOut>(Main);
            }
            else
            {
                throw new InsufficientMemoryException("Size>65535");
            }
        }
        #endregion
    }
}