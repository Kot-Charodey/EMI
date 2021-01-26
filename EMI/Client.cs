using System;
using System.Collections.Generic;
using System.Net;

namespace EMI
{
    using Lower;
    using Lower.Accepter;
    public class Client
    {
        private IMyAccepter Accepter;

        public void Connect(IPAddress IP,int port)
        {
            Accepter = new SimpleAccepter(new IPEndPoint(IP, port));
        }

        public void T1()
        {
            Accepter.Send(new byte[] { 123 }, 1);
        }
        public void T11()
        {
            Accepter.Send(new byte[] { 44 }, 1);
        }

        public void T2()
        {
            Console.WriteLine(Accepter.Receive()[0]);
        }
    }
}
