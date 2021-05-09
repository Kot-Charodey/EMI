using System;
using System.Collections.Generic;

namespace EMI
{
    using Lower.Package;

    /// <summary>
    /// Собирает пакеты в единый
    /// </summary>
    internal class SegmentPackagesBuffer
    {
        /// <summary>
        /// Содержит информацию о собранном пакете а так же массив данных для распаковки
        /// </summary>
        public class ConstructedPackage
        {
            public PacketType PacketType;
            public ulong ID;
            public ulong ReturnID; 
            public ushort RPCAddres;
            public byte[] Data;

            internal ConstructedPackage(Package package)
            {
                PacketType = package.PacketType;
                ID = package.ID;
                ReturnID = package.ReturnID;
                RPCAddres = package.RPCAddres;
                Data = new byte[package.Size];
                for(int i = 0; i < package.Data.Length; i++)
                {
                    Array.Copy(package.Data[i], Data, package.Data[i].Length);
                }
            }
        }

        internal class Package
        {
            public PacketType PacketType;
            public ulong ID;
            public ulong ReturnID; 
            public ushort RPCAddres;
            public ushort RemainedCount;//когда 0 значит пакет собран
            public int Size;
            public HashSet<ushort> SetSegments;//какие сегменты уже пришли
            public byte[][] Data;

            public Package(in BitPacketSegmentedReturned bitPacket)
            {
                PacketType = bitPacket.PacketSegmented.PacketType;
                ID = bitPacket.PacketSegmented.ID;
                ReturnID = bitPacket.ReturnID;
                RPCAddres = bitPacket.PacketSegmented.RPCAddres;
                RemainedCount = bitPacket.PacketSegmented.SegmentCount;
                SetSegments = new HashSet<ushort>(RemainedCount + 1);
                Data = new byte[RemainedCount][];
            }
        }

        private Dictionary<ulong, Package> BufferPackages = new Dictionary<ulong, Package>();

        /// <summary>
        /// Возвращает список недостающик сегментов
        /// </summary>
        /// <param name="ID">Айди пакета</param>
        /// <param name="MaxGetCount">Максимальный размер списка</param>
        /// <returns></returns>
        public ushort[] GetDownloadList(int MaxGetCount,ulong ID)
        {
            Package package = BufferPackages[ID];
            ushort[] need = new ushort[Math.Min(MaxGetCount, package.RemainedCount)];
            int j = 0;

            for (ushort i = 0; i < package.Data.Length; i++)
            {
                if (!package.SetSegments.Contains(i))
                {
                    need[j++] = i;
                }
            }

            return need;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>если null то пакет ещё не готов</returns>
        public ConstructedPackage AddSegment(in BitPacketSegmented bitPacket, byte[] data)
        {
            BitPacketSegmentedReturned bitPacketR = new BitPacketSegmentedReturned()
            {
                PacketSegmented = bitPacket,
            };
            return AddSegment(in bitPacketR, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>если null то пакет ещё не готов</returns>
        public ConstructedPackage AddSegment(in BitPacketSegmentedReturned bitPacket, byte[] data)
        {
            Package package;
            if(!BufferPackages.TryGetValue(bitPacket.PacketSegmented.ID,out package))
            {
                package = new Package(in bitPacket);
            }
            else
            {
                //если данный пакет получен
                if (package.SetSegments.Contains(bitPacket.PacketSegmented.Segment))
                    return null;
            }

            
            package.Data[bitPacket.PacketSegmented.Segment] = data;
            package.SetSegments.Add(bitPacket.PacketSegmented.Segment);
            package.Size += data.Length;
            package.RemainedCount--;

            if (package.RemainedCount == 0)
            {
                BufferPackages.Remove(package.ID);
                return new ConstructedPackage(package);
            }
            return null;
        }
    }
}
