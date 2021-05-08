using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using EMI;
namespace Test1
{
    unsafe class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Пиши 1 Если не сервер");
            RPC.Global.RegisterMethod(0, 0, () =>
            {
                Console.WriteLine("Бух");
            });
            if (Console.ReadLine() == "1")
            {
                Client client = Client.Connect(IPAddress.Parse("31.10.114.169"), 50000);
                while (true)
                {
                    Console.ReadLine();
                    client.RemoteStandardExecution(0);
                }
            }
            else
            {
                Server srv = new Server(50000);
                srv.Start((Client cc) =>
                {
                    Console.WriteLine("Опа");
                });
            }
            while (true)
            {

            }
        }
    }
}
