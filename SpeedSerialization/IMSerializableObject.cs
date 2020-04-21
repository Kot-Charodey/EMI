using System;
using System.Collections.Generic;
using System.Text;

namespace MSerialization
{
    public interface IMSerializableObject
    {
        byte[] Serialization();
        void DeSerialization(byte[] data);
    }
}
