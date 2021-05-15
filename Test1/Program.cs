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
        static void Main()
        {
            Console.WriteLine("Пиши 1 Если не сервер");
            RPC.Global.RegisterMethod(0, 0, () =>
            {
                Console.WriteLine("Бух");
            });
            if (Console.ReadLine() == "1")
            {
                Client client = Client.Connect(IPAddress.Parse("10.20.30.50"), 30000);
                while (true)
                {
                    Console.ReadLine();
                    client.RemoteStandardExecution(0);
                }
            }
            else
            {
                Server srv = new Server(30000);
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