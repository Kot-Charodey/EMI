using System;
using System.Collections.Generic;
using System.Text;

namespace EMI.Lower.Accepter
{
    interface IMyAccepter
    {
        public byte[] Receive();
        public void Send(byte[] buffer, int count);
    }
}
