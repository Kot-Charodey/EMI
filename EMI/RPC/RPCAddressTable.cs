using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
namespace EMI
{
    public class RPCAddressTable
    {
        public ushort ID;
    }

    public class RPCAddress
    {
        public ushort ID;

        public RPCAddress(RPCAddressTable table)
        {
            lock (table)
            {
                ID=table.ID++;
            }
        }
    }

    public class RPCAddress<T1>
    {
        public ushort ID;

        public RPCAddress(RPCAddressTable table)
        {
            lock (table)
            {
                ID=table.ID++;
            }
        }
    }

    public class RPCAddressOut<TOut>
    {
        public ushort ID;

        public RPCAddressOut(RPCAddressTable table)
        {
            lock (table)
            {
                ID = table.ID++;
            }
        }
    }

    public class RPCAddressOut<TOut, T1>
    {
        public ushort ID;

        public RPCAddressOut(RPCAddressTable table)
        {
            lock (table)
            {
                ID=table.ID++;
            }
        }
    }
}

#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена