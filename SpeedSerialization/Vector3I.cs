using System;

namespace MSerialization
{
    public struct Vector3I : IMSerializableObject
    {
        public int x;
        public int y;
        public int z;

        public Vector3I(int X, int Y = 0, int Z = 0)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public Vector3I(byte[] data)
        {
            x = 0;
            y = 0;
            z = 0;
            DeSerialization(data);
        }

        #region IMSerializableObject
        public void DeSerialization(byte[] data)
        {
            x = BitConverter.ToInt32(data, 0);
            y = BitConverter.ToInt32(data, 4);
            z = BitConverter.ToInt32(data, 8);
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
        public static Vector3I operator + (Vector3I a, Vector3I b)
        {
            return new Vector3I(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3I operator - (Vector3I a, Vector3I b)
        {
            return new Vector3I(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3I operator / (Vector3I a, Vector3I b)
        {
            return new Vector3I(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        public static Vector3I operator * (Vector3I a, Vector3I b)
        {
            return new Vector3I(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3I operator / (Vector3I a, int b)
        {
            return new Vector3I(a.x / b, a.y / b, a.z / b);
        }

        public static Vector3I operator * (Vector3I a, int b)
        {
            return new Vector3I(a.x * b, a.y * b, a.z * b);
        }


        public static bool operator == (Vector3I a, Vector3I b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator != (Vector3I a, Vector3I b)
        {
            return a.x != b.x && a.y != b.y && a.z != b.z;
        }
        #endregion

        public override bool Equals(object obj)
        {
            return obj is Vector3I f &&
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