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
            Client client = new Client(NetBaseTCPService.Service);

            //client
            if (args.Length == 0 || args[0].Trim().ToLower()=="client")
            {
                if(args.Length!=0)
                    System.Diagnostics.Process.Start("server.bat");

                client.Disconnected += Client_Disconnected;
                reconect:
                Console.WriteLine("Попытка подключиться...");//"31.10.114.169#25566"
                var status = client.Connect("127.0.0.1#25566", default).Result;
                if (status == false)
                {
                    Console.WriteLine("Не удалось подключиться...");
                    goto reconect;
                }
                Console.WriteLine("Успех");
                Chat(client);
            }//server
            else
            {
                Server server = new Server(NetBaseTCPService.Service);

                var cc = new EMI.DebugServer.EDebuggerServer(server, NetBaseTCPService.Service);
                cc.Start("any#9000");
                Console.WriteLine("Нажмите кнопку что бы запустить сервер");
                Console.ReadLine();

                server.Start("any#25566");
                Console.WriteLine("Ожидание клиента");
                rep:
                try
                {
                    client = server.Accept().Result;
                    client.Disconnected += Client_Disconnected;
                    Console.WriteLine("Готово");
                    Chat(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    goto rep;
                }
            }
        }

        private static void Chat(Client client)
        {
            var msg = new Indicator.Func<string>("MSG");
            client.RPC.RegisterMethod(MSG, msg);
            msg.RCall("боба", client).Wait();

            while (true)
            {
                string txt = Console.ReadLine();
                if (txt == "exit")
                {
                    client.Disconnect("я так захотел");
                    System.Threading.Thread.Sleep(3000);
                    break;
                }
                msg.RCall(txt, client).Wait();
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
