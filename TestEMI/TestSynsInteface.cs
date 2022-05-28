using EMI;
using EMI.SyncInterface;
using System;

namespace TestEMI
{
    public interface IChat
    {
        //int a { get; set; }

        [OnlyClient]
        void SMS();
        [OnlyClient]
        void SMS(int a);
    }

    public class Chat : IChat
    {
        //public int a { get => 1; set => throw new NotImplementedException(); }

        public void SMS()
        {
            Console.WriteLine($"Сообщение:");
        }

        public void SMS(int a)
        {

        }
    }

    public static class TestSynsInteface
    {
        public static IChat InitFull(Client cc)
        {

            var chatIndicator = new SyncInterface<IChat>("MyChat");
            var chat = chatIndicator.NewIndicator(cc);
            chat.SMS(15);
            //SyncInterface.RegisterInterface<IChat>(new Chat(), cc.RPC, "MyChat");
            return null;
        }
    }
}
