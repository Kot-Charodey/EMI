using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMI;

namespace TestEMI
{
    public interface IChat
    {
        void SMS(string text);
    }

    public class Chat : IChat
    {
        public void SMS(string txt)
        {
            Console.WriteLine($"Сообщение: {txt}");
        }
    }

    public static class TestSynsInteface
    {
        public static IChat InitFull(Client cc)
        {
            var chat = SyncInterface.CreateIndicator<IChat>(cc, "MyChat");
            SyncInterface.RegisterInterface<IChat>(new Chat(), cc.RPC, "MyChat");
            return chat;
        }
    }
}
