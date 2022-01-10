using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using EMI;
using EMI.Network;

namespace Test1
{
    class Program
    {
        static void Main(string[] args)
        {
            EMI.ProBuffer.ProArrayBuffer ProArray = new EMI.ProBuffer.ProArrayBuffer(10, 1024);

            //client
            if (args.Length == 0)
            {
                NetBaseTCP.NetBaseTCPClient client = new NetBaseTCP.NetBaseTCPClient
                {
                    ProArrayBuffer = ProArray
                };
                client.Disconnected += Client_Disconnected;
                var resul = client.Сonnect("31.10.114.169#25566", default).Result;
                Console.WriteLine(resul);
                PP(client);
            }
            else
            {
                NetBaseTCP.NetBaseTCPServer server = new NetBaseTCP.NetBaseTCPServer
                {
                    ProArrayBuffer = ProArray
                };
                server.StartServer("any#25566");
                Console.WriteLine("Wait client");
                var client = server.AcceptClient().Result;
                Console.WriteLine("Done");
                PP(client);
            }
        }

        private static void Client_Disconnected(string error)
        {
            System.Windows.Forms.MessageBox.Show("Клиент отключился, код ошибки:\n" + error);
            Environment.Exit(0);
        }

        static void PP(INetworkClient client)
        {
            Task.Run(async () =>
              {
                  while (true)
                  {
                      try
                      {
                          var array = (await client.AcceptAsync(default)).Array;
                          Console.WriteLine(Encoding.Unicode.GetString(array.Bytes, 0, array.Length));
                          array.Release();
                      }
                      catch
                      {
                      }
                  }
              });

            while (true)
            {
                string str = Console.ReadLine();
                if(str=="exit")
                {
                    client.Disconnect("пошёл нахуй");
                    break;
                }
                byte[] arr = Encoding.Unicode.GetBytes(str);
                var array = new EMI.ProBuffer.WrapperArray(arr);
                client.Send(array, true);
            }
        }
    }
}
