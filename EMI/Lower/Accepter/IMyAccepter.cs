using System.Net;

namespace EMI.Lower.Accepter
{
    interface IMyAccepter
    {
        byte[] Receive();
        void Send(byte[] buffer, int count);
        void Stop();
        EndPoint EndPoint { get; }
    }
}
