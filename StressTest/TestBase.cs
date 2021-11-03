using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Net;
using EMI;

namespace StressTest
{
    public abstract class TestBase
    {
        private RPCAddressTable RPCAddressTable = new RPCAddressTable();

        protected abstract void Init(RPCAddressTable table);

        /// <summary>
        /// Вызывается при подключение игрока к серверу
        /// </summary>
        /// <param name="client"></param>
        protected abstract void ServerNewUser(Client client);
        /// <summary>
        /// Вызывается при подключении к серверу
        /// </summary>
        /// <param name="client"></param>
        protected abstract void ClientProcess(Client client);

        public void Run()
        {
            Init(RPCAddressTable);
            Console.WriteLine("Ентер если сервер");
            if (Console.ReadLine().Length == 0)
            {
                Server server = new Server(25600);
                server.Start((client) =>
                {
                    Console.WriteLine("new user");
                    ServerNewUser(client);
                });

                Console.WriteLine("server working\n\n");
                Thread.Sleep(-1);
            }
            else
            {
                Console.WriteLine("Connecting...");
                Client client = Client.Connect(IPAddress.Parse("31.10.114.169"), 25600).Result;
                if (client == null)
                {
                    Console.WriteLine("Error!");
                    Thread.Sleep(1000);
                    return;
                }
                Console.WriteLine("Done");
                ClientProcess(client);
            }
        }
    }
}
