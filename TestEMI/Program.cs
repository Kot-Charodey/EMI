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
                client = new Client(NetBaseTCPService.Service);
                client.Disconnected += Client_Disconnected;
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
                client.Disconnected += Client_Disconnected;
                Console.WriteLine("Готово");
            }

            var msg = Indicator.Create<string>("MSG");
            client.RPC.RegisterMethod<string>(MSG, msg);
            client.RPC.RegisterForwarding(msg, (Client cc) => { return new Client[] { cc }; });

            while (true)
            {
                string txt = Console.ReadLine();
                if (txt == "exit")
                {
                    client.Disconnect("я так захотел");
                    break;
                }
                msg.RCall(txt, client, RCType.Forwarding).Wait();
            }
        }

        private static void Client_Disconnected(string error)
        {
            Console.WriteLine("Client_Disconnected => " + error);
        }

        static void MSG(string txt)
        {
            Console.WriteLine($"Сообщение: {txt}");
        }
    }
}
