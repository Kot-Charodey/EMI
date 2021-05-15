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
            Console.WriteLine("Пиши 1 Если не сервер");
            RPC.Global.RegisterMethod(0, 0, () =>
            {
                Console.WriteLine("Бух");
            });
            RPC.Global.RegisterMethod(1, 0, (string txt) =>
            {
                System.Windows.Forms.MessageBox.Show(txt,"MSG");
            });
            RPC.Global.RegisterMethod(2, 0, Console.Beep);
            RPC.Global.RegisterMethod(3, 0, GetInput);

            if (Console.ReadLine() == "1")
            {
                string a = "10.20.30.50";
                string b = "31.10.114.169";
                Client client = Client.Connect(IPAddress.Parse(b), 30000);
                Console.WriteLine("Опа");
                TestCom(client);
            }
            else
            {
                Server srv = new Server(30000);
                srv.Start((Client cc) =>
                {
                    Console.WriteLine("Опа");
                    TestCom(cc);
                });
            }
            while (true)
            {

            }
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