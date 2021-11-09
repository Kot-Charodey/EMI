using System;
using System.Collections.Generic;

namespace EMI.Lower
{
    using Package;

    /// <summary>
    /// Хранит сегментные большие пакеты (только до их полной отправки и запроса об окончательном получении)
    /// </summary>
    internal class SendSegmentBackupBuffer
    {
        private readonly Dictionary<ulong, PacketInfo> BufferPackages = new Dictionary<ulong, PacketInfo>();

        public class PacketInfo
        {
            public byte[] Bytes;
            public bool IsReturned;
            public BitPacketSegmentedReturned BPSR;
            public BitPacketSegmented BPS;

            public PacketInfo(byte[] bytes, BitPacketSegmentedReturned bPSR)
            {
                Bytes = bytes;
                IsReturned = true;
                BPSR = bPSR;
            }

            public PacketInfo(byte[] bytes, BitPacketSegmented bPS)
            {
                Bytes = bytes;
                IsReturned = false;
                BPS = bPS;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="data">Все данные целиком (без пакета)</param>
        /// <param name="BPSR">Первый отправленный пакет</param>
        public void Add(ulong ID, byte[] data, BitPacketSegmentedReturned BPSR)
        {
            lock (BufferPackages)
                BufferPackages.Add(ID, new PacketInfo(data, BPSR));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="data">Все данные целиком (без пакета)</param>
        /// <param name="BPS">Первый отправленный пакет</param>
        public void Add(ulong ID, byte[] data, BitPacketSegmented BPS)
        {
            lock (BufferPackages)
                BufferPackages.Add(ID, new PacketInfo(data, BPS));
        }

        /// <summary>
        /// Вынимает указанный сегмент пакета
        /// </summary>
        /// <param name="ID">Айди пакета</param>
        /// <param name="segment">номер сегмента</param>
        /// <param name="segmentData">данные сегмента</param>
        /// <returns></returns>
        public unsafe PacketInfo Get(ulong ID, ushort segment,out byte[] segmentData)
        {

            lock (BufferPackages)
                if (BufferPackages.TryGetValue(ID, out var data))
                {
                    int point = segment * 1024;
                    segmentData = new byte[Math.Min(data.Bytes.Length - point, 1024)];
                    Array.Copy(data.Bytes, point, segmentData, 0, segmentData.Length);
                    return data;
                }
                else
                {
                    segmentData = null;
                    return null;
                }
        }

        public void Remove(ulong ID)
        {
            //без защиты так как запрос гарантированно не дублируемый
            //if(BufferPackages.ContainsKey(ID))
            lock (BufferPackages)
                BufferPackages.Remove(ID);
        }
    }
}