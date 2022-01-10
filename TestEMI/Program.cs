using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetBaseTCP;
using EMI;
using EMI.Indicators;

namespace TestEMI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Client client = null;

            //client
            if (args.Length == 0)
            {
                //System.Diagnostics.Debugger.Launch();
                client = new Client(NetBaseTCPService.Service);
            reconect:
                Console.WriteLine("Попытка подключиться...");
                var status = client.Connect("31.10.114.169#25566", default).Result;
                if (status == false)
                {
                    Console.WriteLine("Не удалось подключиться...");
                    goto reconect;
                }
                Console.WriteLine("Успех");

            }//server
            else
            {
                Server server = new Server(NetBaseTCPService.Service);
                server.Start("any#25566");
                Console.WriteLine("Ожидание клиента");
                client = server.Accept().Result;
                Console.WriteLine("Готово");
            }

            client.RPC.RegisterMethod(MSG);
            Indicator msg = new Indicator(client.RPC.Factory, MSG);


            while (true)
            {
                string txt = Console.ReadLine();
                client.Invoke(msg, RPCInvokeInfo.Guarantee).Wait();
            }
        }

        static void MSG(MethodHandle handle)
        {
            Console.WriteLine($"Пинг:{handle.Ping.TotalMilliseconds}");
        }
    }
}
