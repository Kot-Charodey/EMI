using System;

namespace MSerialization
{
    public struct Vector3F : IMSerializableObject
    {
        public float x;
        public float y;
        public float z;

        public Vector3F(float X, float Y = 0, float Z = 0)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public Vector3F(byte[] data)
        {
            x = 0;
            y = 0;
            z = 0;
            DeSerialization(data);
        }

        #region IMSerializableObject
        public void DeSerialization(byte[] data)
        {
            x = BitConverter.ToSingle(data, 0);
            y = BitConverter.ToSingle(data, 4);
            z = BitConverter.ToSingle(data, 8);
        }

        public byte[] Serialization()
        {
            byte[] _x = BitConverter.GetBytes(x);
            byte[] _y = BitConverter.GetBytes(y);
            byte[] _z = BitConverter.GetBytes(z);

            byte[] arr = new byte[4 * 3];
            Array.Copy(_x, 0, arr, 0, 4);
            Array.Copy(_y, 0, arr, 4, 4);
            Array.Copy(_z, 0, arr, 8, 4);

            return arr;
        }
        #endregion

        #region Operators
        public static Vector3F operator +(Vector3F a, Vector3F b)
        {
            return new Vector3F(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3F operator -(Vector3F a, Vector3F b)
        {
            return new Vector3F(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3F operator /(Vector3F a, Vector3F b)
        {
            return new Vector3F(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        public static Vector3F operator *(Vector3F a, Vector3F b)
        {
            return new Vector3F(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3F operator /(Vector3F a, float b)
        {
            return new Vector3F(a.x / b, a.y / b, a.z / b);
        }

        public static Vector3F operator *(Vector3F a, float b)
        {
            return new Vector3F(a.x * b, a.y * b, a.z * b);
        }


        public static bool operator ==(Vector3F a, Vector3F b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(Vector3F a, Vector3F b)
        {
            return a.x != b.x && a.y != b.y && a.z != b.z;
        }
        #endregion

        public override bool Equals(object obj)
        {
            return obj is Vector3F f &&
                   x == f.x &&
                   y == f.y &&
                   z == f.z;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }
    }
}