using System;
using System.Runtime.InteropServices;

namespace EMI.Lower
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct BitPacketSimple
    {
        [FieldOffset(0)]
        public PacketType PacketType;
        [FieldOffset(1)]
        public ushort ByteDataLength;
        [FieldOffset(3)]
        public fixed byte ByteData[1024];//1027

        public BitPacketSimple(PacketType packetType)
        {
            PacketType = packetType;
            ByteDataLength = 0;
        }

        public BitPacketSimple(PacketType packetType,int byteDataLength)
        {
            PacketType = packetType;
            ByteDataLength = (ushort)byteDataLength;
        }

        public BitPacketSimple(PacketType packetType, byte[] data)
        {
            PacketType = packetType;
            ByteDataLength = (ushort)data.Length;

            //по моему мнению самый быстрый способ запихать массив в фиксированный массив
            fixed (byte* source = &data[0])
            {
                fixed (byte* destination = ByteData)
                {
                    Buffer.MemoryCopy(source, destination, 1024, data.Length);
                }
            }
        }
        
        public int GetSizeOf()
        {
            return 3 + ByteDataLength;
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
