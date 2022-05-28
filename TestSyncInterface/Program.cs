using EMI;
using NetBaseTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMI.SyncInterface;

namespace TestSyncInterface
{
    public interface ITest
    {
        [OnlyClient]
        void WaitAndGet(int a);
    }

    class MyTest : ITest
    {
        public void WaitAndGet(int a)
        {
            Console.WriteLine("Ждём");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Run(args);
        }

        private static void Run(string[] args)
        {
            Client client = new Client(NetBaseTCPService.Service);
            var sync = new SyncInterface<ITest>("MyTest");

            //client
            if (args.Length == 0)
            {
                Server server = new Server(NetBaseTCPService.Service);
                server.Start("any#25566");

                MyTest test = new MyTest();
                sync.RegisterClass(server, test);

                Console.WriteLine("Ожидание клиента");
                client = server.Accept().Result;
                client.Disconnected += Client_Disconnected;
                Console.WriteLine("Готово");

                Console.ReadLine();
            }//server
            else
            {
                System.Diagnostics.Process.Start("server.bat");

                client = new Client(NetBaseTCPService.Service);
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

                var ob = sync.NewIndicator(client);
                Console.WriteLine("Ждём");
                ob.WaitAndGet(15);
                Console.ReadLine();
                //Console.WriteLine(data);
            }
        }

        private static void Client_Disconnected(string error)
        {
            Console.WriteLine("Client_Disconnected => " + error);
        }
    }
}
