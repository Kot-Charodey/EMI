using EMI;
using EMI.SynsInteface;
using System;

namespace TestEMI
{
    public interface IChat
    {
        //int a { get; set; }

        [OnlyClient]
        void SMS();
    }

    public class Chat : IChat
    {
        //public int a { get => 1; set => throw new NotImplementedException(); }

        public void SMS()
        {
            Console.WriteLine($"Сообщение:");
        }
    }

    public static class TestSynsInteface
    {
        public static IChat InitFull(Client cc)
        {

            var chatIndicator = new SynsInterface<IChat>("MyChat");
            var chat = chatIndicator.NewIndicator(cc);
            chat.SMS();
            //SyncInterface.RegisterInterface<IChat>(new Chat(), cc.RPC, "MyChat");
            return null;
        }
    }
}
