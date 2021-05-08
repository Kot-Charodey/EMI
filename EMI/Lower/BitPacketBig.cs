using System;
using System.Runtime.InteropServices;

namespace EMI.Lower
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct BitPacketBig
    {

        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ushort ByteDataLength;
        [FieldOffset(3)]
        public ulong ID;
        [FieldOffset(11)]
        public ulong MainID;
        [FieldOffset(19)]
        public ushort Segment;
        [FieldOffset(21)]
        public ushort SegmentCount;
        [FieldOffset(23)]
        public fixed byte ByteData[1024];

        public BitPacketBig(PacketType packetType, ulong id,ulong mainID, byte* buffer,int startIndex,int count, ushort segment, ushort segmentCount)
        {
            PacketType = packetType;
            ByteDataLength = (ushort)count;
            ID = id;
            MainID = mainID;
            Segment = segment;
            SegmentCount = segmentCount;

            //пум-пурум-пум-пум пихаем как можно быстрее но это не точно
            fixed (byte* destination = ByteData)
            {
                Buffer.MemoryCopy(buffer + startIndex, destination, count, count);
            }
        }

        public int GetSizeOf()
        {
            return 23 + ByteDataLength;
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

        public void GetAllBytes(byte[] buffer)
        {

            //пум-пурум-пум-пум пихаем как можно быстрее но это не точно
            fixed (void* source = &PacketType)
            {
                fixed (void* destination = &buffer[0])
                {
                    Buffer.MemoryCopy(source, destination, buffer.Length, buffer.Length);
                }
            }
        }
    }
}
