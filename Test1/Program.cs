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
            Console.WriteLine("Пиши 1 Если ты человек\nПиши 2 Если ты Паша\nЖми Enter если ты сервер");
            RPC.Global.RegisterMethod(0, 0, Bux);
            RPC.Global.RegisterMethod<string>(1, 0, MSG);
            RPC.Global.RegisterMethod(2, 0, Console.Beep);
            RPC.Global.RegisterMethod(3, 0, GetInput);
            RPC.Global.RegisterMethod<int[]>(4, 0, TestArray);

            string com = Console.ReadLine();

            if (com == "1")
            {
                string b = "31.10.114.169";
                Client client = Client.Connect(IPAddress.Parse(b), 30000);
                Console.WriteLine("Опа");
                TestCom(client);
            }
            else if(com == "2")
            {
                string a = "10.20.30.50";
                Client client = Client.Connect(IPAddress.Parse(a), 30000);
                Console.WriteLine("Опа");
                TestCom(client);
            }
            else
            {
                Server srv = new Server(30000);
                srv.Start(Proc);
            }
            while (true)
            {

            }
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
                        cc.RemoteStandardExecution(1, Microsoft.VisualBasic.Interaction.InputBox("Введите сообщение", "MSG"));
                        break;
                    case "beep":
                        cc.RemoteStandardExecution(2);
                        break;
                    case "gi":
                        new Thread(() =>
                        {
                            System.Windows.Forms.MessageBox.Show(cc.RemoteGuaranteedExecution<string>(3), "gi");
                        }).Start();
                        break;
                    case "at":
                        int[] b = new int[5000];
                        for (int i = 0; i < b.Length; i++)
                        {
                            b[i] = i;
                        }
                        cc.RemoteGuaranteedExecution(4,b);
                        break;
                    default:
                        cc.RemoteStandardExecution(0);
                        break;
                }
        }

        static string GetInput()
        {
            return Microsoft.VisualBasic.Interaction.InputBox("Вас просят ввести сообщение", "GI");
        }
    }
}