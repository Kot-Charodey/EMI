using System.Net;

namespace EMI.Lower.Accepter
{
    interface IMyAccepter
    {
        int Receive(byte[] buffer);
        void Send(byte[] buffer, int count);
        void Stop();
        EndPoint EndPoint { get; }
    }
}
