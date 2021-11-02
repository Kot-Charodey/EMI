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
    class Program
    {
        static RPCAddressTable RPCAddressTable=new RPCAddressTable();
        static RPCAddressOut<int, int[]> SumR = new RPCAddressOut<int, int[]>(RPCAddressTable);

        static void Main(string[] args)
        {
            RPC.Global.RegisterMethod(SumR, 0, Sum);

            Console.WriteLine("введи что нибудь если не сервер");
            if (Console.ReadLine().Length == 0)
            {
                Server server = new Server(25600);
                server.Start((client) =>
                {
                    Console.WriteLine("new user");
                });

                Console.WriteLine("server working\n\n");
                Thread.Sleep(-1);
            }
            else
            {
                Client client = Client.Connect(IPAddress.Parse("31.10.114.169"), 25600).Result;
                client.RequestRatePing = RequestRatePing.ms10;

                Stopwatch stopwatch = new Stopwatch();
                long workCounter = 0;

                int paral=0;
                bool error = false;
                stopwatch.Start();

                Thread thread = new Thread(() =>
                  {
                      int[] arr = new int[5000];
                      int s = 0;
                      for (int i = 0; i < arr.Length; i++)
                      {
                          s += arr[i] = i;
                      }

                      while (true)
                      {
                          int num = client.RemoteGuaranteedExecution(SumR, arr).Result;

                          if (num == s)
                          {
                              workCounter++;
                          }
                          else
                          {
                              error = true;
                          }
                      }

                  });
                thread.Start();

                while (true)
                {
                    List<string> lines = new List<string>();

                    double work = workCounter / stopwatch.Elapsed.TotalSeconds;

                    lines.Add($"Ping: {client.PingMS}");
                    if(error)
                        lines.Add($"Внимание неверные данные!");
                    lines.Add($"Запросов в секунду: {work}");
                    lines.Add($"Всего запросов: {workCounter}");
                    lines.Add($"Отправленно полезных данных за секунду {work * 5001 * sizeof(int) / 1024 / 1024} Мегабайт");
                    lines.Add($"Всего полезных данных {workCounter * 5001 * sizeof(int) / 1024 / 1024 / 1024} Гигабайт");

                    for (int l = 0; l < lines.Count; l++)
                    {
                        for (int i = lines[l].Length; i < Console.WindowWidth - 1; i++)
                        {
                            lines[l] += " ";
                        }
                    }
                    string str = "";
                    foreach (var line in lines)
                    {
                        str += line + "\n";
                    }
                    Console.SetCursorPosition(0, 0);
                    Console.Write(str);
                    Thread.Sleep(10);
                }
            }
        }

        static int Sum(int[] data)
        {
            int s = 0;
            for (int i = 0; i < data.Length; i++)
            {
                s += data[i];
            }
            return s;
        }
    }
}