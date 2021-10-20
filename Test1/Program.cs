﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using EMI;

namespace Test1
{
    unsafe class Program
    {
        private static readonly RPCAddressTable table = new RPCAddressTable();

        public static readonly RPCAddress               BuxA         = new RPCAddress(table);
        public static readonly RPCAddress<string>       MSGA         = new RPCAddress<string>(table);
        public static readonly RPCAddress               BeepA        = new RPCAddress(table);
        public static readonly RPCAddressOut<string>    GetInputA    = new RPCAddressOut<string>(table);
        public static readonly RPCAddress<int[]>        TestArrayA   = new RPCAddress<int[]>(table);

        static readonly CancellationTokenSource CancellationTokenSource=new CancellationTokenSource();

        static Server srv;

        static void Main()
        {
            Console.WriteLine("Пиши 1 Если ты человек\nПиши 2 Если ты Паша\nЖми Enter если ты сервер");

            RPC.Global.RegisterMethod(BuxA, 0, Bux);
            RPC.Global.RegisterMethod(MSGA, 0, MSG);
            RPC.Global.RegisterMethod(BeepA, 0, Console.Beep);
            RPC.Global.RegisterMethod(GetInputA, 0, GetInput);
            RPC.Global.RegisterMethod(TestArrayA, 0, TestArray);

            string com = Console.ReadLine();
            try
            {
                if (com == "1")
                {
                    string b = "31.10.114.169";
                    Client client = Client.Connect(IPAddress.Parse(b), 25600);
                    client.CloseEvent += Client_CloseEvent;
                    Console.WriteLine("Опа");
                    TestCom(client);
                }
                else if (com == "2")
                {
                    string a = "10.20.30.50";
                    Client client = Client.Connect(IPAddress.Parse(a), 25600);
                    client.CloseEvent += Client_CloseEvent;
                    Console.WriteLine("Опа");
                    TestCom(client);
                }
                else
                {
                    srv = new Server(25600);
                    srv.Start(Proc);
                }

                Task.Delay(-1, CancellationTokenSource.Token).Wait();
            }
            catch { }

            Console.ReadLine();
        }

        private static void Client_CloseEvent(CloseType obj)
        {
            CancellationTokenSource.Cancel();
        }

        private static void TestArray(int[] p1)
        {
            Console.WriteLine("Test Array length:"+p1.Length);
            for(int i = 0; i < p1.Length; i++)
            {
                if (p1[i] != i)
                {
                    Console.WriteLine("error->" + i);
                    return;
                }
            }
        }

        static void Proc(Client cc)
        {
            Console.WriteLine("Опа");
            cc.CloseEvent += Client_CloseEvent;
            TestCom(cc);
        }


        static void Bux()
        {
            Console.WriteLine("Бух");
        }

        static void MSG(string txt)
        {
            System.Windows.Forms.MessageBox.Show(txt, "MSG");
        }

        static void TestCom(Client cc)
        {
            while (true)
                switch (Console.ReadLine().ToLower())
                {
                    case "msg":
                        cc.RemoteStandardExecution(MSGA, Microsoft.VisualBasic.Interaction.InputBox("Введите сообщение", "MSG"));
                        break;
                    case "beep":
                        cc.RemoteStandardExecution(BeepA);
                        break;
                    case "gi":
                        new Thread(() =>
                        {
                            System.Windows.Forms.MessageBox.Show(cc.RemoteGuaranteedExecution(GetInputA).Result, "gi");
                        }).Start();
                        break;
                    case "at":
                        int[] b = new int[5000];
                        for (int i = 0; i < b.Length; i++)
                        {
                            b[i] = i;
                        }
                        cc.RemoteGuaranteedExecution(TestArrayA,b);
                        break;
                    default:
                        cc.RemoteStandardExecution(BuxA);
                        break;
                }
        }

        static string GetInput()
        {
            return Microsoft.VisualBasic.Interaction.InputBox("Вас просят ввести сообщение", "GI");
        }
    }
}