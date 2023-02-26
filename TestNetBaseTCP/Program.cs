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
            //client
            if (args.Length == 0)
            {
                NetBaseTCP.NetBaseTCPClient client = new NetBaseTCP.NetBaseTCPClient();
                client.Disconnected += Client_Disconnected;
                var resul = client.Сonnect("127.0.0.1#25566", default).Result;
                Console.WriteLine(resul);
                PP(client);
            }
            else
            {
                NetBaseTCP.NetBaseTCPServer server = new NetBaseTCP.NetBaseTCPServer();
                server.StartServer("any#25566");
                Console.WriteLine("Wait client");
                var client = server.AcceptClient(default).Result;
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
                  bool r = true;
                  while (r)
                  {
                      try
                      {
                          var array = (await client.AcceptPacket(1024, default));
                          Console.WriteLine(Encoding.Unicode.GetString(array.Bytes, 0, array.Length));
                          array.Dispose();
                      }catch(Exception ee)
                      {
                          Console.WriteLine("Отвалился ридер данных\n" + ee.ToString());
                      }
                      finally
                      {
                          r = false;
                      }
                  }
              });

            while (true)
            {
                string str = Console.ReadLine();
                if(str=="exit")
                {
                    client.Disconnect("пошёл нахуй");
                    System.Threading.Thread.Sleep(5000);
                    break;
                }
                byte[] arr = Encoding.Unicode.GetBytes(str);
                var array = new EMI.NGC.EasyArray(arr);
                client.Send(array, true, default);
            }
        }
    }
}
