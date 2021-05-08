using System;
using System.Runtime.InteropServices;

namespace EMI.Lower
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct BitPacketMedium
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ushort ByteDataLength;
        [FieldOffset(3)]
        public ulong ID;
        [FieldOffset(FieldOffsetDataStart)]
        public fixed byte ByteData[1024];//1028

        public const int FieldOffsetData = 10;
        public const int FieldOffsetDataStart = FieldOffsetData + 1;

        public BitPacketMedium(PacketType packetType, ulong id, int byteDataLength)
        {
            PacketType = packetType;
            ByteDataLength = (ushort)byteDataLength;
            ID = id;
        }

        public int GetSizeOf()
        {
            return FieldOffsetData + ByteDataLength;
        }

        public byte[] GetByteData()
        {
            byte[] data = new byte[ByteDataLength];

            //пум-пурум-пум-пум пихаем как можно быстрее но это не точно
            fixed (byte* source = ByteData)
            {
                fixed (byte* destination = &data[0])
                {
                    Buffer.MemoryCopy(source, destination, data.Length, data.Length);
                }
            }

            return data;
        }

        public byte[] GetAllBytes()
        {
            byte[] data = new byte[GetSizeOf()];

            //пум-пурум-пум-пум пихаем как можно быстрее но это не точно
            fixed (void* source = &PacketType)
            {
                fixed (void* destination = &data[0])
                {
                    Buffer.MemoryCopy(source, destination, data.Length, data.Length);
                }
            }

            return data;
        }
    }
}