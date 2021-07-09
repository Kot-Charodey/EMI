//using System;
//using System.Net.Sockets;
//using System.Net;
//using System.IO.Pipes;
//
//namespace EMI.Lower.Accepter
//{
//    internal class SimpleAccepterPipe : IMyAccepter
//    {
//        private readonly PipeStream Client;
//        public EndPoint EndPoint { get; private set; }
//
//        public SimpleAccepterPipe(string address)
//        {
//            Client = new AnonymousPipeClientStream(PipeDirection.InOut, address);
//        }
//
//        private readonly byte[] tmp=new byte[1248];
//
//        public byte[] Receive(out int size)
//        {
//            @while:
//            size = Client.(tmp, 0, tmp.Length);
//
//            if (Point.Equals(EndPoint))
//            {
//                return tmp;
//            }
//            goto @while;
//        }
//
//        public void Send(byte[] buffer, int count)
//        {
//            Client.SendTo(buffer, count,SocketFlags.None, EndPoint);
//        }
//
//        public void Stop()
//        {
//            Client.Dispose();
//        }
//    }
//}
//