using System;
using System.Collections.Generic;
using SpeedByteConvector;

namespace EMI
{
    using Lower;
    using Lower.Package;

    internal class SegmentPackagesBuffer
    {
        private class BigPackage
        {
            public byte[] buffer;
            public ushort Size = 0;
            public ushort RemainedCount;
        }

        private Dictionary<ulong, BigPackage> BufferPackages = new Dictionary<ulong, BigPackage>();


        /// <summary>
        /// 
        /// </summary>
        /// <returns>если null то пакет ещё не готов</returns>
        public byte[] AddSegment(in BitPacketBig bitPacket)
        {
            BigPackage package;

            if (BufferPackages.TryGetValue(bitPacket.MainID, out package))
            {

            }
            else
            {
                package = new BigPackage();
                package.buffer = new byte[bitPacket.SegmentCount * 1024];
                package.RemainedCount = bitPacket.SegmentCount;

                BufferPackages.Add(bitPacket.MainID, package);
            }

            package.Size += bitPacket.ByteDataLength;

            unsafe
            {
                fixed (byte* bff = &package.buffer[bitPacket.Segment * 1024])
                {
                    fixed (byte* sou = bitPacket.ByteData)
                    {
                        Buffer.MemoryCopy(sou, bff, bitPacket.ByteDataLength, bitPacket.ByteDataLength);
                    }
                }
            }

            package.RemainedCount--;
            if (package.RemainedCount == 0)
            {
                BufferPackages.Remove(bitPacket.MainID);
                return package.buffer;
            }
            else
            {
                return null;
            }
        }
    }
}
